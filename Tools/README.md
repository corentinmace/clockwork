# Clockwork Tools

Ce dossier contient les outils externes utilisés par Clockwork.

## ndstool.exe

**Source**: https://github.com/DS-Pokemon-Rom-Editor/DSPRE/tree/master/DS_Map/Tools

Outil pour extraire et manipuler les fichiers ROM Nintendo DS.

### Utilisation
```bash
ndstool -x <rom.nds> -9 arm9.bin -7 arm7.bin -y9 y9.bin -y7 y7.bin -d data -y overlay -t banner.bin -h header.bin
```

### Note
Vous devez télécharger ndstool.exe depuis le repository DSPRE et le placer dans ce dossier.
Le fichier n'est pas inclus dans le repository pour des raisons de licence.

Lien: https://github.com/DS-Pokemon-Rom-Editor/DSPRE/raw/master/DS_Map/Tools/ndstool.exe
