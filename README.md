POM_SAG-V.3 - Outil de Transfert de Données
📌 Description
POM_SAG est une application de bureau conçue pour faciliter le transfert des données entre diverses sources (API POM et Dynamics 365 Finance & Operations) et une base de données SQL Server destinataire. Cette application permet la récupération, la transformation et l'enregistrement des données commerciales essentielles telles que les produits, les clients, les commandes et autres informations commerciales.
🚀 Fonctionnalités principales

Récupération de données depuis plusieurs sources:

API RESTful POM (Clients, Commandes, Produits, LignesCommandes)
API Dynamics 365 Finance & Operations (ReleasedProductsV2 et autres entités)


Filtrage des données par date:

Possibilité de filtrer les données à transférer par plage de dates


Sécurité:

Authentification par clé API pour l'API POM
Authentification OAuth pour l'API Dynamics 365


Stockage flexible:

Enregistrement des données en format JSON dans une base SQL Server


Interface conviviale:

Interface graphique intuitive développée avec Windows Forms
Suivi en temps réel des opérations de transfert
Journal des opérations pour audit et débogage



🔧 Prérequis techniques

.NET 9.0 ou supérieur
Windows 10/11 ou Windows Server 2016 ou supérieur
Accès aux APIs (POM et/ou Dynamics 365)
SQL Server (version 2016 ou supérieure) pour la base de données de destination

📥 Installation

Clonez ce dépôt ou téléchargez l'archive:

bashCopygit clone https://github.com/votreorganisation/POM_SAG-V.3.git

Ouvrez la solution dans Visual Studio 2022 ou supérieur:

CopyPOM_SAG-V.3.sln

Restaurez les packages NuGet nécessaires:

bashCopydotnet restore

Compilez le projet:

bashCopydotnet build --configuration Release

Exécutez l'application:

bashCopydotnet run --project POMsag/POMsag.csproj
⚙️ Configuration
L'application utilise un fichier config.ini qui sera créé automatiquement au premier lancement dans le répertoire de l'application. Vous devrez configurer les paramètres suivants:
Section [Settings]

ApiUrl: URL de l'API POM
ApiKey: Clé d'authentification pour l'API POM
DatabaseConnectionString: Chaîne de connexion vers la base de données SQL Server destinataire

Section [D365]

TokenUrl: URL pour l'obtention du jeton OAuth (ex: https://login.microsoftonline.com/{tenant-id}/oauth2/token)
ClientId: Identifiant de l'application dans Azure AD
ClientSecret: Secret de l'application dans Azure AD
Resource: URL de ressource Dynamics 365
DynamicsApiUrl: URL de l'API Dynamics 365
MaxRecords: Nombre maximal d'enregistrements à récupérer (0 pour aucune limite)
SpecificItemNumber: Filtrer par numéro d'article spécifique (optionnel)

🔄 Processus de transfert

Sélectionnez le type de données à transférer (Clients, Commandes, Produits, ReleasedProductsV2, etc.)
Activez ou désactivez le filtrage par date si nécessaire
Cliquez sur "Démarrer le transfert"
L'application récupère les données depuis la source appropriée
Les données sont transformées au format JSON
Les données sont enregistrées dans la table JSON_DAT de la base SQL destinataire
Un rapport de succès est affiché avec le nombre d'enregistrements transférés

🗂️ Structure du projet
CopyPOMsag/
├── Models/            # Modèles de données
│   ├── ReleasedProduct.cs
│   └── ...
├── Services/          # Services métier
│   ├── DynamicsApiService.cs    # Connexion à l'API Dynamics 365
│   ├── LoggerService.cs         # Service de journalisation
│   └── ...
├── AppConfiguration.cs   # Gestion de la configuration
├── Form1.cs              # Formulaire principal
├── ConfigurationForm.cs  # Formulaire de configuration
├── Program.cs            # Point d'entrée de l'application
└── ...
📋 Logging
L'application génère des logs détaillés dans le fichier pom_api_log.txt situé dans le répertoire de l'application. Ces logs incluent:

Les requêtes effectuées
Les réponses reçues (tronquées pour les grandes quantités de données)
Les erreurs rencontrées
Les opérations de base de données réussies

🔒 Sécurité

Les clés API et secrets sont stockés localement dans le fichier de configuration
Aucune information sensible n'est transmise en dehors des canaux sécurisés
Les connexions aux APIs utilisent des mécanismes d'authentification sécurisés (API Keys, OAuth 2.0)

⚠️ Résolution des problèmes courants

Erreur de connexion à l'API POM:

Vérifiez que l'URL et la clé API sont correctes dans la configuration
Assurez-vous que le serveur API est en fonctionnement


Erreur d'authentification Dynamics 365:

Vérifiez les informations ClientId, ClientSecret et TokenUrl
Assurez-vous que l'application dispose des permissions appropriées


Erreur de connexion à la base de données:

Vérifiez la chaîne de connexion dans la configuration
Assurez-vous que le serveur SQL est accessible et que l'utilisateur dispose des droits nécessaires


Aucune donnée récupérée:

Vérifiez les filtres de date si activés
Consultez les logs pour identifier d'éventuelles erreurs
Vérifiez le paramètre MaxRecords dans la configuration



🛠️ Technologies utilisées

C# .NET 9.0
Windows Forms pour l'interface utilisateur
HTTP Client pour les communications API
Microsoft.Data.SqlClient pour les connexions SQL Server
INI-Parser pour la gestion de la configuration
System.Text.Json pour le traitement JSON

🔄 Mise à jour
Pour mettre à jour l'application vers une nouvelle version:

Sauvegardez votre fichier de configuration config.ini
Téléchargez ou clonez la dernière version
Remplacez le fichier config.ini par votre fichier sauvegardé
Compilez et exécutez la nouvelle version