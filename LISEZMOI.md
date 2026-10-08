# File System OS

**File System OS Company** — créé par **Vincent Senut**.
Système d'exploitation écrit en C# avec Cosmos.

## 1. Installer File System OS (démarrage SANS l'ISO ni la clé USB)

L'outil `Outils/creer-disque.sh` fabrique un **disque complet et démarrable** :
partition FAT32, noyau, chargeur de démarrage **Limine**, et l'environnement `\FSOS`
déjà installé (applications FSL, documents). Une fois ce disque en place, l'ISO et la clé
USB ne servent plus à rien.

Il tourne sous **Linux** ou sous **Windows avec WSL** (Ubuntu) :

```bash
sudo apt install fdisk dosfstools mtools git build-essential p7zip-full python3 qemu-utils
cd FileSystemOS/Outils
./creer-disque.sh /chemin/vers/FileSystemOS.iso
```

Il produit :

| Fichier | Pour |
|---|---|
| `FileSystemOS-disque.vdi` | VirtualBox |
| `FileSystemOS-disque.vmdk` | VMware |
| `FileSystemOS-disque.img` | QEMU, ou copie sur un vrai disque |

Option : `--taille 2048` pour un disque de 2 Go (1 Go par défaut).

### a) VirtualBox
1. Nouvelle VM : type *Other / Other/Unknown*, 512 Mo de RAM.
2. **Stockage → Contrôleur IDE** : ajouter `FileSystemOS-disque.vdi` comme disque dur.
3. **Retirer l'ISO** du lecteur CD (ou supprimer le lecteur CD).
4. Système : ordre d'amorçage **Disque dur** en premier. Souris **PS/2**, écran **VBoxVGA**.
5. Démarrer : Limine affiche « File System OS » puis le système démarre depuis le disque.

### b) VMware
Créer une VM *Other 32-bit*, puis « Utiliser un disque virtuel existant » → `FileSystemOS-disque.vmdk`
(contrôleur **IDE**). Pas d'ISO nécessaire.

### c) QEMU
```bash
qemu-system-i386 -m 512 -hda FileSystemOS-disque.img
```

### d) Sur le disque interne d'un vrai ordinateur (avec une clé USB)
Cosmos n'a pas de pilote USB : File System OS lui-même ne peut pas lire la clé.
On utilise donc une **clé USB Linux « live »** (Ubuntu par exemple) pour copier le disque :

1. Démarrer le PC sur la clé Ubuntu (« Essayer Ubuntu »).
2. Copier le dossier `FileSystemOS` et l'ISO sur la clé ou les télécharger, puis :
   ```bash
   lsblk                                   # repérer le disque interne, ex : /dev/sda
   sudo ./creer-disque.sh FileSystemOS.iso --vers /dev/sda
   ```
   Il faut taper **OUI** : le disque interne est **entièrement effacé**.
3. Retirer la clé USB et redémarrer : le PC démarre directement sur File System OS.

> Le PC doit démarrer en mode **BIOS / Legacy (CSM)**, et le **Secure Boot** doit être désactivé.
> Le disque interne doit être en mode **IDE / compatible** (ou AHCI si ta version de Cosmos le gère).

### Le nom du projet

Dans le fichier `.csproj` du projet Cosmos (nommé **FileSystemOS**), ajoute :

```xml
<PropertyGroup>
  <AssemblyName>FileSystemOS</AssemblyName>
  <Product>File System OS</Product>
  <Company>File System OS Company</Company>
  <Authors>Vincent Senut</Authors>
  <Version>1.0</Version>
</PropertyGroup>
```

L'ISO produite s'appellera alors `FileSystemOS.iso`.

## 2. Installateur intégré (depuis File System OS)

Menu → **Installer File System OS**. Il sert à (ré)installer ou réparer l'environnement `\FSOS`
sur un lecteur, à vider un lecteur, ou à formater un disque entier. Il ne rend pas le disque
démarrable à lui seul : pour cela, utilise `creer-disque.sh` (section 1).

### Options de l'installateur

- **Lecteur existant + « Installer »** : ajoute `\FSOS` sans rien effacer.
- **Lecteur existant + case « Supprimer tous les dossiers et fichiers »** : vide entièrement le lecteur
  (suppression par lots de 25 éléments, l'OS reste fluide), puis installe.
- **« Disque N entier »** : efface la table de partitions, recrée une partition, la formate en FAT32, puis installe.
  Fonctionne sur un disque vierge comme sur un disque déjà utilisé.

Chaque action destructrice demande une confirmation (« Confirmer ? »).

### Si l'installation échoue dans VirtualBox

Erreur **« failed to find an unallocated directory entry »** : le pilote FAT32 de Cosmos
n'arrive pas à écrire sur ce disque (formatage incompatible ou disque trop grand).
La solution la plus fiable : un disque **VHD de 1 Go formaté en FAT32 par Windows**.

Dans un invite de commandes Windows **administrateur**, tape `diskpart`, puis :

```
create vdisk file="C:\VMs\fsos.vhd" maximum=1024 type=fixed
select vdisk file="C:\VMs\fsos.vhd"
attach vdisk
convert mbr
create partition primary
format fs=fat32 quick label=FSOS
detach vdisk
exit
```

Dans VirtualBox : **Configuration → Stockage → Contrôleur IDE** → retire les autres disques durs,
ajoute `fsos.vhd`, garde l'ISO dans le lecteur CD. Démarre, choisis **« 0:\ … pret »**,
**décoche** « Supprimer tous les dossiers… », puis **Installer**.

## 3. Démarrage de l'OS

Au lancement, le noyau prépare seulement l'écran, la souris et l'ordonnanceur, puis lance le
processus **demarrage**. Il affiche le **logo** — un V invisible dont on ne voit que des points
gris qui glissent le long de son tracé — et charge les modules un par un, chacun dans son fichier
(`Demarrage/Modules/`) et avec ses propres processus :

| # | Module | Processus créés |
|---|---|---|
| 1 | Ouverture du noyau | `noyau` (thread `gardien` : mesure tours/s et images/s) |
| 2 | Mémoire et accélérateur | — |
| 3 | Système de fichiers | — |
| 4 | Fichiers système internes | — (lit `\FSOS\Systeme\*.cfg`) |
| 5 | Accélération (caches) | — (police et icônes précalculées) |
| 6 | Horloge | `horloge` |
| 7 | Réseau | `reseau` (DHCP) |
| 8 | Cogestion des threads | `cogestion` (thread `rendus`) |
| 9 | Interface graphique | `affichage`, `interface`, `attente` |
| 10 | Clavier et souris | `systeme` |
| 11 | Console et commandes | — |
| 12 | Menu démarrer | — (applications FSL) |
| 13 | Extinction et redémarrage | `alimentation` |
| 14 | Applications | une fenêtre = un processus |

Commande `demarrage` dans la console : journal complet du chargement.
L'arrêt et le redémarrage passent par le processus `alimentation`, qui affiche le même logo.

### Cogestion threads / processus / buffers

Les fenêtres gardent leur processus et leur thread, mais ne dessinent plus elles-mêmes :
elles déposent une demande dans le **buffer de cogestion**. Le thread `rendus` en traite au plus
`rendus_par_tour` par tour (la fenêtre active d'abord). Les processus ont une **priorité** :
haute et normale à chaque tour, basse un tour sur quatre. Commandes `ps` et `perf`.

### Fichiers internes (`\FSOS\Systeme`)

| Fichier | Réglages |
|---|---|
| `demarrage.cfg` | `duree_etape` (images par étape ; 1 = démarrage le plus rapide) |
| `interface.cfg` | `attente_auto`, `fenetres_actives_max`, `rendus_par_tour` |
| `console.cfg` | `message_accueil` |
| `reseau.cfg` | `dhcp` |
| `horloge.cfg` | `decalage_heures` (ex : 2 en France l'été si la VM est en UTC) |

Recréés automatiquement s'ils manquent. Commande `config` pour les afficher.
Réseau dans VirtualBox : carte **PCnet-FAST III** en mode NAT.

## 4. Écran, images et fichiers externes

### Résolution adaptée à chaque ordinateur
Au démarrage, `Systeme/Materiel.cs` détecte le **processeur**, la **mémoire**, la **carte graphique**
et la **liste des résolutions** qu'elle accepte, puis choisit une résolution sûre :
1. celle demandée dans `demarrage.cfg` (`resolution=1280x720`), si l'écran la gère ;
2. sinon, en mode `auto` : 1280x720, 1366x768, 1280x800, 1024x768 puis 800x600,
   la première acceptée par la carte **et** compatible avec la mémoire disponible ;
3. sinon un repli de sécurité (1024x768, 800x600, 640x480), puis le mode par défaut du pilote.

Si un mode échoue, le suivant est essayé : plus d'écran noir au démarrage.
La fiche est écrite dans `\FSOS\Systeme\materiel.txt`.
Menu → **Affichage** pour changer la résolution (appliquée au redémarrage), ou `resolution 1280x720`.
Commande `materiel` pour voir la détection.

### Écran rayé / lignes en biais sur un vrai PC
Sur un vrai ordinateur, c'est le BIOS (VBE) qui règle l'écran au démarrage ; Cosmos ne peut pas le changer
ensuite. Si l'OS change quand même la résolution, la carte reste dans son mode d'origine et l'image part
en diagonale (rayures vertes / bleues, texte rouge illisible).
- Depuis la version 1.0.1, sur un vrai PC l'OS **garde le mode d'origine** (voir `Systeme/Materiel.cs`).
- Dans le `.csproj`, demander un mode sûr au démarrage :
  ```xml
  <CompileVBEMultiboot>True</CompileVBEMultiboot>
  <VBEResolution>1024x768x32</VBEResolution>
  ```
- BIOS (ex. Dell) : Boot List Option = **Legacy**, **Enable Legacy Option ROMs** coché, Secure Boot désactivé.
- Pour forcer malgré tout une résolution : `resolution=force:1024x768` dans `demarrage.cfg`.

### Images et fond d'écran
- Format lu : **BMP 24 ou 32 bits** (Paint → « Enregistrer sous » → BMP 24 bits).
- Double-clic sur un `.bmp` dans l'explorateur → visionneuse **Images** → bouton **Fond d'écran**.
- Menu → Affichage → **Retirer le fond** pour revenir au dégradé. Commande `fond`.
- `creer-disque.sh` installe un fond par défaut : `\FSOS\Images\fond.bmp`.

### Clé USB : ce qui est possible
Cosmos **n'a pas de pilote USB de stockage** : brancher une vraie clé USB sur un PC où tourne
File System OS ne la fera pas apparaître. Ce n'est pas réglable dans le code de l'OS
(il faudrait écrire tout un pilote USB : contrôleurs xHCI/EHCI + stockage de masse).

En attendant, deux solutions :
- **Machine virtuelle** : `Outils/creer-cle-virtuelle.sh mes_fichiers/` crée une « clé virtuelle »
  FAT32 (`cle.vdi` / `cle.vmdk`). Branche-la comme **2e disque IDE** : elle apparaît dans l'explorateur
  (ex : `1:\`). Les `.png`/`.jpg` sont convertis en `.bmp` automatiquement. Les `.txt` s'ouvrent
  dans l'éditeur, les `.bmp` dans Images.
- **Vrai PC** : un 2e disque dur (ou une carte SD sur un lecteur interne SATA/IDE) formaté en FAT32
  est reconnu de la même façon.

### À propos
Menu → **À propos** (ou commande `apropos`) : logo animé, version, éditeur,
**créateur : Vincent Senut**, et les mentions « Copie interdite de l'OS » et
« OS Open Source : veuillez mentionner la version et l'OS en cas de toute modification ».

## 5. Mode Live, comptes et navigateur

### Mode Live (sans installation)
Démarre simplement sur l'ISO ou la clé USB : si File System OS n'est installé sur aucun disque,
il passe en **mode Live**. Un **disque en mémoire** (64 Mo, FAT16, créé par `Systeme/RamDisk.cs`)
reçoit tout l'environnement `\FSOS` : on peut créer des textes, des images, des comptes,
lancer des programmes FSL… sans rien installer. La barre des tâches affiche « LIVE ».

**Où sont gardés les fichiers ?**
- S'il existe un disque FAT32 (disque virtuel de la VM, 2e disque, clé virtuelle…), la session y est
  **sauvegardée** dans `\FSOS-Live` à chaque arrêt (et via Menu → *Sauvegarder la session*, ou `sauver`),
  puis **restaurée** au démarrage suivant.
- **Sur la clé USB elle-même : impossible.** Une fois l'OS lancé, Cosmos n'a pas de pilote USB et ne voit
  plus la clé qui l'a démarré. Sans autre disque FAT32, les fichiers du mode Live sont perdus à l'arrêt.

### Comptes et mots de passe
- Premier démarrage : création du **compte administrateur**. Ensuite, chaque démarrage (et Menu → *Verrouiller*)
  affiche l'**écran de connexion** : rien n'est accessible avant.
- Menu → **Comptes** : créer / supprimer des comptes (administrateur), changer son mot de passe.
- Fichier interne `\FSOS\Systeme\comptes.cfg` : seule l'**empreinte SHA-256** (avec sel) du mot de passe
  est stockée, jamais le mot de passe. Chaque compte a un dossier `\FSOS\Utilisateurs\<nom>`.
- Limite : le disque n'est pas chiffré ; quelqu'un qui lit le disque depuis un autre système voit les fichiers.

### Navigateur internet
Menu → **Navigateur internet** (ou `web <adresse ou recherche>`).
- Navigateur **texte** : titres, paragraphes, listes, liens cliquables, retour, actualiser, accueil.
- **HTTP uniquement** : Cosmos n'a pas de chiffrement TLS. Les adresses `https://` et les recherches passent
  automatiquement par **FrogFind** (frogfind.com), un service qui simplifie les pages modernes pour les vieux
  navigateurs et les renvoie en HTTP.
- Pas de JavaScript, pas de CSS, pas d'images, pas de connexion à des comptes en ligne.
- Réseau requis : carte **PCnet-FAST III** en NAT dans VirtualBox. Pendant le chargement d'une page,
  le système est brièvement figé (la pile réseau de Cosmos est bloquante).

### Téléchargements, PDF et TextWar
- **Navigateur** : un lien vers un fichier qui n'est pas une page (PDF, image PNG/BMP/JPG, fichier…) est
  **téléchargé** dans `\FSOS\Utilisateurs\<nom>\Telechargements`, puis ouvert avec le bon logiciel.
  Les `[image]` des pages sont des liens : un clic télécharge l'image (si elle est servie en http).
- **Lecteur PDF** : affiche le **texte** de chaque page (pas la mise en page ni les images), navigation
  page par page, bouton « Enregistrer en .txt ». Il décompresse les PDF lui-même (`Utils/Inflate.cs`,
  DEFLATE écrit à la main) et lit les tables ToUnicode des polices. Les PDF scannés (images) n'ont pas de texte.
- **Images** : PNG (toutes variantes non entrelacées) et BMP. Le JPEG est téléchargé mais pas encore affiché.
- **TextWar** : télécharge des **documents texte**. Donne une adresse (http, https) ou des mots-clés :
  page web, fichier `.txt` ou même PDF sont convertis en texte propre (78 colonnes) et enregistrés en `.txt`.
  La liste des documents téléchargés s'ouvre d'un clic dans l'éditeur.
- Limite commune : les fichiers **https** ne peuvent pas être téléchargés directement (pas de TLS dans Cosmos) ;
  les **pages** https passent par FrogFind, mais pas les images ni les PDF.

## 6. Interface : fenêtres, attente et suppression

- Toutes les icônes sont écrites en hexadécimal : boutons de titre (attente ‖, réduire —, fermer ✕),
  icône de chaque fenêtre (titre, barre des tâches, menu) et boutons des barres d'outils.
- **Barre du haut « En attente »** : le bouton ‖ d'une fenêtre l'y épingle. Son processus est
  **gelé** (l'ordonnanceur ne l'exécute plus) et elle n'est plus composée à l'écran.
  Clic sur la puce = reprendre. Boutons à droite : tout mettre en attente / tout reprendre.
- **Anti-saturation** : au-delà de 4 fenêtres visibles, la plus ancienne (jamais celle qui a le focus)
  part automatiquement en attente (`WaitBuffer.AutoBalance`, réglable).
- **Explorateur** : bouton corbeille ou touche **Suppr** pour supprimer un fichier ou un dossier
  (avec tout son contenu), après confirmation (Entrée = oui, Échap = non).

## 7. Applications externes

Tout fichier `.fsl` (ou `.fsc` compilé) placé dans `\FSOS\Applications`
apparaît automatiquement dans le menu (« App : nom ») et s'y lance dans son propre processus.

## 8. File Syst Langage (FSL)

```
// Commentaire
int x = 5;            // entier
texte nom = "Bob";    // texte
int a = 0;            // le nombre zéro
int b = 0.nul;        // variable NULLE (s'écrit aussi 0/nul)
int c;                // sans valeur : nulle aussi

x = x + 1;
afficher("x = " + x);      // affiche aussi les nulles : "0/nul"

si (b == 0/nul) {
    afficher("b est nulle");
} sinon si (x > 3) {
    afficher("grand");
} sinon {
    afficher("petit");
}

tantque (x < 10) {
    x = x + 1;
}
```

- Opérateurs : `+ - * / %`, `== != < > <= >=`, `&& || !`, `vrai`, `faux`.
- `+` avec un texte fait une concaténation.
- Calculer avec une variable nulle provoque une erreur claire : *calcul avec une variable nulle (0/nul)*.
- Les blocs `{ }` ne créent pas de nouvelle portée : toutes les variables sont globales.

### Compilation

FSL est **compilé en bytecode** pour une machine virtuelle à pile :

- Éditeur → **Compiler** (ou `fslc fichier.fsl` dans la console) produit `fichier.fsc`.
- **Exécuter** (F5) compile en mémoire puis lance le programme.
- Chaque programme tourne dans son propre processus (`fsl-nom`), 300 instructions par tour :
  une boucle infinie ne bloque pas l'OS, et `kill <pid>` l'arrête.
