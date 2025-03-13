# Documentation POM SAG - Version 4bis

## Table des matières

1. [Introduction](#introduction)
2. [Architecture de l'application](#architecture-de-lapplication)
3. [Configuration requise](#configuration-requise)
4. [Installation et configuration](#installation-et-configuration)
5. [Fonctionnalités principales](#fonctionnalités-principales)
6. [Structure du code](#structure-du-code)
7. [Services](#services)
8. [Formulaires](#formulaires)
9. [Modèles de données](#modèles-de-données)
10. [Procédures et workflows](#procédures-et-workflows)
11. [Gestion des erreurs](#gestion-des-erreurs)
12. [Maintenance et dépannage](#maintenance-et-dépannage)
13. [Guide de développement futur](#guide-de-développement-futur)

## Introduction

POM SAG est une application de transfert de données permettant de récupérer des informations depuis différentes sources de données (API POM et Dynamics 365) et de les stocker dans une base de données SQL Server locale.

L'application dispose d'une interface graphique intuitive qui permet aux utilisateurs de sélectionner les données à transférer, d'appliquer des filtres par date et de suivre l'état du transfert en temps réel.

La version 4bis apporte une refonte complète de l'architecture avec une amélioration significative de l'expérience utilisateur, une meilleure gestion des erreurs et une configuration plus flexible.

## Architecture de l'application

L'application suit une architecture en couches :

1. **Couche Présentation** : Formulaires Windows Forms pour l'interface utilisateur
2. **Couche Services** : Classes de services qui encapsulent la logique métier
3. **Couche Modèles** : Classes de modèles de données représentant les entités manipulées
4. **Couche Infrastructure** : Services de journalisation et de configuration

L'application utilise plusieurs services pour interagir avec les différentes sources de données :

- `GenericApiService` pour les appels API génériques
- `DynamicsApiService` pour les appels spécifiques à Dynamics 365
- `SchemaAnalysisService` pour l'analyse des structures de données
- `LoggerService` pour la journalisation

## Configuration requise

- **.NET** : .NET 9.0 ou supérieur
- **Système d'exploitation** : Windows 10/11
- **Base de données** : SQL Server (Express ou Standard)
- **Accès réseau** : Connexion internet pour accéder aux API externes
- **Droits** : Droits administratifs locaux pour la première exécution

## Installation et configuration

### Installation

1. Copiez le dossier `POM_SAG-V.4bis` à l'emplacement souhaité
2. Exécutez le fichier `POMsag.exe` pour lancer l'application

### Configuration initiale

Lors du premier lancement, un fichier de configuration par défaut (`config.ini`) sera créé avec les paramètres suivants :

```ini
[Settings]
ApiUrl=http://localhost:5001/
ApiKey=
DatabaseConnectionString=Server=192.168.9.13\SQLEXPRESS;Database=pom;User Id=eurodislog;Password=euro;TrustServerCertificate=True;Encrypt=False

[D365]
TokenUrl=https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token
ClientId=
ClientSecret=
Resource=https://br-uat.sandbox.operations.eu.dynamics.com/
DynamicsApiUrl=https://br-uat.sandbox.operations.eu.dynamics.com/data
MaxRecords=500
SpecificItemNumber=

[FieldSelections]
Preferences={}
```

Vous devez accéder à la configuration via le menu "Fichier > Configuration" pour renseigner :

- Les informations de connexion à l'API POM
- Les informations de connexion à Dynamics 365
- Les paramètres de la base de données cible

## Fonctionnalités principales

### 1. Configuration centralisée

Le menu "Fichier > Configuration" permet d'accéder aux formulaires de configuration pour :

- API POM
- Dynamics 365
- Base de données
- Sélection des champs à transférer

### 2. Sélection des sources de données

L'interface principale permet de sélectionner :

- La table de données à transférer
- L'option de filtrage par date
- Les dates de début et de fin si le filtrage est activé

### 3. Transfert de données

Le bouton "Démarrer le transfert" lance le processus de transfert avec :

- Barre de progression indiquant l'avancement
- Panneau de logs détaillant chaque étape
- Gestion des erreurs avec messages explicatifs

### 4. Gestion des logs

- Visualisation des logs en temps réel dans l'application
- Option "Voir les logs" pour consulter l'historique complet
- Journalisation détaillée des erreurs avec contexte

### 5. Sélection des champs

Possibilité de configurer les champs à inclure dans le transfert pour chaque entité :

- Interface de sélection intuitive avec cases à cocher
- Découverte automatique des champs disponibles
- Persistance des préférences entre les sessions

## Structure du code

### Namespaces principaux

- `POMsag` : Namespace racine contenant les formulaires et la classe de configuration
- `POMsag.Models` : Classes de modèles de données
- `POMsag.Services` : Services d'accès aux données et utilitaires

### Fichiers principaux

- `Program.cs` : Point d'entrée de l'application
- `Form1.cs` / `Form1.Designer.cs` : Formulaire principal
- `AppConfiguration.cs` : Gestion de la configuration
- `ConfigurationForm.cs` : Interface de configuration
- `LoggerService.cs` : Service de journalisation
- `GenericApiService.cs` : Service d'accès aux API
- `DynamicsApiService.cs` : Service spécifique à Dynamics 365
- `SchemaAnalysisService.cs` : Service d'analyse des structures de données
- `FieldSelectionForm.cs` : Interface de sélection des champs
- `ErrorHandlingService.cs` : Gestion centralisée des erreurs

## Services

### AppConfiguration

**Fichier** : `POM_SAG-V.4bis/POMsag/AppConfiguration.cs`

Service de gestion de la configuration qui :

- Charge les paramètres depuis le fichier `config.ini`
- Crée un fichier de configuration par défaut si nécessaire
- Sauvegarde les modifications de configuration
- Gère les préférences de sélection des champs

### LoggerService

**Fichier** : `POM_SAG-V.4bis/POMsag/Services/LoggerService.cs`

Service de journalisation qui :

- Écrit les messages dans un fichier `pom_api_log.txt`
- Journalise les exceptions avec leur contexte
- Utilise un verrou pour gérer les accès concurrents
- Fournit des méthodes pour effacer les logs

### GenericApiService

**Fichier** : `POM_SAG-V.4bis/POMsag/Services/GenericApiService.cs`

Service générique pour l'accès aux API qui :

- Supporte différentes sources de données (POM et Dynamics)
- Gère les paramètres de filtrage par date
- Retourne les données sous forme de `List<Dictionary<string, object>>`
- Délègue les appels spécifiques à Dynamics au `DynamicsApiService`

### DynamicsApiService

**Fichier** : `POM_SAG-V.4bis/POMsag/Services/DynamicsApiService.cs`

Service spécifique pour l'accès à Dynamics 365 qui :

- Gère l'authentification OAuth
- Construit des requêtes OData adaptées
- Récupère les produits publiés
- Gère la mise en cache du token d'accès

### SchemaAnalysisService

**Fichier** : `POM_SAG-V.4bis/POMsag/Services/SchemaAnalysisService.cs`

Service d'analyse des structures de données qui :

- Découvre les champs disponibles dans les API
- Prend en charge les différentes sources de données
- Retourne un ensemble de noms de champs
- Utilisé par le formulaire de sélection des champs

### ErrorHandlingService

**Fichier** : `POM_SAG-V.4bis/POMsag/Services/ErrorHandlingService.cs`

Service de gestion des erreurs qui :

- Analyse le type d'exception
- Fournit des messages d'erreur contextuels
- Affiche des boîtes de dialogue adaptées
- Propose des solutions potentielles selon le type d'erreur

## Formulaires

### Form1 (Formulaire principal)

**Fichiers** :

- `POM_SAG-V.4bis/POMsag/Form1.cs`
- `POM_SAG-V.4bis/POMsag/Form1.Designer.cs`

Interface principale permettant :

- La sélection des tables à transférer
- Le filtrage par date
- Le lancement du transfert
- La visualisation du statut et des logs

### ConfigurationForm

**Fichier** : `POM_SAG-V.4bis/POMsag/ConfigurationForm.cs`

Formulaire de configuration avec onglets pour :

- Paramètres généraux (API POM et base de données)
- Paramètres Dynamics 365
- Sélection des champs à transférer

### FieldSelectionForm

**Fichier** : `POM_SAG-V.4bis/POMsag/FieldSelectionForm.cs`

Formulaire de sélection des champs qui :

- Découvre automatiquement les champs disponibles
- Permet de sélectionner/désélectionner les champs
- Sauvegarde les préférences dans la configuration

## Modèles de données

### ReleasedProduct

**Fichier** : `POM_SAG-V.4bis/POMsag/Models/ReleasedProduct.cs`

Modèle représentant un produit publié dans Dynamics 365 :

- Propriétés fortement typées pour les champs principaux
- Dictionnaire `AdditionalProperties` pour les champs dynamiques
- Méthodes utilitaires pour la conversion et l'accès aux propriétés

### FieldSelectionPreference

**Fichier** : `POM_SAG-V.4bis/POMsag/Models/FieldSelectionPreference.cs`

Modèle pour stocker les préférences de sélection des champs :

- Nom de l'entité
- Dictionnaire associant les noms de champs à leur état de sélection

### ApiConfiguration et ApiEndpoint (Version 4.0)

Ces modèles sont utilisés dans la version 4.0 et pourraient être intégrés dans la version 4bis à l'avenir.

## Procédures et workflows

### Transfert de données

1. L'utilisateur sélectionne une table dans la liste déroulante
2. Optionnellement, il active le filtrage par date et définit une plage
3. Il clique sur "Démarrer le transfert"
4. L'application :
   - Récupère les données depuis la source appropriée
   - Filtre les champs selon les préférences configurées
   - Enregistre les données dans la base de données locale
   - Met à jour la barre de progression et les logs
   - Affiche un message de succès ou d'erreur

### Configuration des champs

1. L'utilisateur ouvre le formulaire de configuration
2. Il sélectionne l'onglet "Sélection des champs"
3. Il choisit une source de données et une entité
4. Il clique sur "Configurer les champs"
5. L'application :
   - Analyse la structure de données de l'entité
   - Affiche tous les champs disponibles
   - Charge l'état de sélection précédent
   - L'utilisateur coche/décoche les champs souhaités
   - Les préférences sont sauvegardées pour les prochains transferts

## Gestion des erreurs

L'application implémente une gestion robuste des erreurs à plusieurs niveaux :

1. **Formulaires** : Affichage de messages d'erreur contextuels
2. **Services** : Journalisation détaillée des exceptions
3. **Transfert** : Affichage de la progression et des erreurs en temps réel
4. **Configuration** : Validation des saisies et messages d'erreur explicites

Le `ErrorHandlingService` analyse le type d'exception pour fournir des messages adaptés :

- Erreurs HTTP : Problèmes de communication avec les API
- Erreurs SQL : Problèmes de base de données
- Erreurs de désérialisation : Problèmes de format de données

## Maintenance et dépannage

### Fichiers de log

Le fichier `pom_api_log.txt` contient toutes les opérations et erreurs survenues. Il peut être consulté via le menu "Fichier > Voir les logs" ou directement dans le répertoire de l'application.

### Fichier de configuration

Le fichier `config.ini` peut être sauvegardé pour conserver les paramètres entre les installations.

### Problèmes courants

1. **Erreur de connexion à l'API POM**

   - Vérifier l'URL et la clé API dans la configuration
   - S'assurer que le serveur API est accessible

2. **Erreur d'authentification Dynamics 365**

   - Vérifier le Client ID et Client Secret
   - S'assurer que les droits d'accès sont corrects

3. **Erreur de base de données**
   - Vérifier la chaîne de connexion
   - S'assurer que le serveur SQL est accessible et que la base existe

## Guide de développement futur

### Évolutions possibles

1. **Intégration complète des améliorations de la V4.0**

   - Gestionnaire d'API configurable
   - Configuration des endpoints dynamique

2. **Améliorations de l'interface utilisateur**

   - Thème sombre/clair
   - Redimensionnement dynamique des contrôles
   - Internationalisation

3. **Fonctionnalités supplémentaires**
   - Programmation des transferts
   - Transferts incrémentiels
   - Exportation des données en différents formats

### Extension du code

Pour ajouter une nouvelle source de données :

1. Créer un nouveau service spécifique dans le dossier `Services`
2. Mettre à jour `GenericApiService` pour prendre en charge la nouvelle source
3. Ajouter les paramètres de configuration dans `AppConfiguration`
4. Mettre à jour l'interface utilisateur pour exposer la nouvelle source
