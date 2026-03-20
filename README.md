# 🚀 E-Commerce Microservices Ecosystem (.NET Core 10)

Je m'appelle Franklin KUETE (Architecte de Solution), et je vous présente mon écosystème **Cloud-Native** ultra-performant.  
Ce projet est une démonstration d'architecture distribuée moderne, mettant l'accent sur la **résilience**, l'**observabilité**, la **séparation stricte des domaines métier**, tout ca dans une **Clean Architecture** et **Microservice**

---

## 📖 1. Description du Projet
J'ai concu cette plateforme de test pour gèrer le cycle de vie complet d'un système e-commerce (**Clients, Produits, Commandes**).  
Elle est conçue pour supporter une charge importante grâce à :
- Une gestion fine du cache  
- Une communication asynchrone  
- Une isolation totale des données  

---

## 🛠 2. Stack Technologique & Écosystème

| Composant              | Technologie / Approche |
|-------------------------|-------------------------|
| Framework               | 🚀 .NET 10 (ASP.NET Core API) |
| API Gateway             | 🚪 YARP (Reverse Proxy) avec Polly Resilience |
| Bases de Données        | 🗄️ SQL Server 2025, PostgreSQL 16, MySQL 8.3, MariaDB 10.11 |
| Messaging               | 📡 Apache Kafka & MassTransit |
| Caching                 | ⚡ Redis 7.2 (Distribué L2) + IMemoryCache (Local L1) |
| Mapping & Validation    | 🔄 Mapster & ✅ FluentValidation |
| Pattern                 | ⚔️ CQRS (Command Query Responsibility Segregation avec MediatR) + 🧩 Clean Architecture (Domain, Application, Infrastructure, UI) |
| Style                   | 🌐 Microservice Architecture (services découplés, scalables, indépendants) + 📡 Event-Driven Architecture (Kafka + MassTransit pour communication asynchrone) |
| Conteneurisation        | 🐳 Docker-Compose (orchestration multi-services, reproductibilité, isolation) |
| Observabilité (détails) | 🔦 Tempo & OpenTelemetry : Tracing distribué de bout en bout (Gateway → Kafka → DB)<br>📈 Prometheus & Grafana : Visualisation des métriques de santé et de performance<br>🪵 Loki & Seq : Centralisation des logs structurés<br>🩺 Healthchecks : Sondes de démarrage et de disponibilité pour chaque service et base de données |


---

## 🧬 3. Styles & Patterns d’Architecture
Le projet implémente les standards les plus rigoureux de l'industrie :

- **Clean Architecture** : Découplage total entre Domain, Application et Infrastructure  
- **CQRS** : Séparation des Commandes et des Requêtes via MediatR  
- **Event-Driven Architecture (EDA)** : Communication asynchrone pour un couplage faible entre services  
- **Database-per-Service** : Autonomie technologique et isolation des pannes  
- **Unit of Work & Repository** : Gestion atomique des transactions SQL  

---

## 📦 4. Présentation des Microservices

| Service     | Base de Données | Rôle Principal |
|-------------|-----------------|----------------|
| ApiGateway  | SQL Server      | Yarp Reverse Proxy, Authentification JWT, Identity, Routage & Sécurité, Gestion des utilisateurs, Roles et token |
| ClientApi   | PostgreSQL      | Gestion des client |
| ProductApi  | MySQL           | Catalogue produits et synchronisation des stocks via Kafka |
| CommandeApi | MariaDB         | Orchestration des commandes et émission d'événements |

---

## 📡 5. Communication Inter-Services
Le système utilise un modèle hybride pour maximiser la disponibilité :

- **Synchrone (YARP/HTTP)** : Pour les lectures directes et le routage client  
- **Asynchrone (Kafka)** : Flux haute performance (ex : mise à jour des stocks après une commande)  
- **Asynchrone (MassTransit)** : Gestion des messages transactionnels avec politiques de Retry  

---

## ⛓️ 6. Pipeline de Traitement Unifié (MediatR)
Chaque requête traverse une chaîne de Behaviors :

1. 🕒 Metrics : Mesure de la latence globale  
2. 📝 Logging : Traçabilité via TraceId (Serilog)  
3. ✅ Validation Request : Rejet immédiat si le format est invalide (FluentValidation)  
4. 🗂️ Cache Check : Retour immédiat si la donnée est présente en L1/L2  
5. 🔐 Transaction : Ouverture du scope SQL pour les Commands  
6. ⚖️ Business Validation : Vérification des règles métier complexes en base  
7. 🧹 Cache Invalidation : Nettoyage automatique des clés liées en cas de modification  

---

## ⚡ 7. Stratégie de Caching Hybride
Le **HybridCacheService** résout le problème de latence réseau :

- **L1 (Local)** : Stocké en RAM (Vitesse éclair 🚀)  
- **L2 (Redis)** : Partagé entre les instances (Cohérence 🤝)  
- **Pub/Sub Invalidation** : Lorsqu'une donnée change, Redis notifie toutes les instances pour vider leur cache L1 local instantanément  

---

## 🛡️ 8. Sécurité & Validation
- **Gateway Security** : Centralisation de l'Identity et injection du UserContext dans les headers  
- **Validation à 2 niveaux** :  
  - Request Validation : Forme et syntaxe (FluentValidation)  
  - Business Validation : Sémantique et état du système (IBusinessValidation)  
- **Global Error Handling** : Middleware interceptant toutes les exceptions pour un format de réponse `ApiResponse<T>` unique  

---

## 📊 9. Observabilité (Stack LGT+S)
Le monitoring est au cœur de l'infrastructure
- 🔦 **Tempo & OpenTelemetry** : Tracing distribué de bout en bout *(Gateway → Kafka → DB)*  
- 📈 **Prometheus & Grafana** : Visualisation des métriques de santé et de performance  
- 🪵 **Loki & Seq** : Centralisation des logs structurés  
- 🩺 **Healthchecks** : Sondes de démarrage et de disponibilité pour chaque service et base de données
