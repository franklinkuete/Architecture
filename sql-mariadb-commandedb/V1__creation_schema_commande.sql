
-- 1. Table Commande
CREATE TABLE IF NOT EXISTS Commande (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    DateCommande DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    libelle VARCHAR(255) NOT NULL,
    Description VARCHAR(255) NOT NULL,
    Statut INT NOT NULL DEFAULT 0, -- Enum StatutCommande (0 = Validate, 1 = Cancel)
    ClientId VARCHAR(100) NOT NULL, -- Référence externe (ex: GUID ou ID Microservice Client)
    INDEX idx_client_id (ClientId)
) ENGINE=InnoDB;

-- 2. Table ProductCommande
CREATE TABLE IF NOT EXISTS ProductCommande (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NomProduit VARCHAR(255) NOT NULL,
    ProduitId VARCHAR(100) NOT NULL, -- Référence externe vers le catalogue produit
    Quantite INT NOT NULL CHECK (Quantite > 0),
    PrixUnitaire DECIMAL(18, 2) NOT NULL,
    
    -- Relation avec la table Commande
    -- Note : Changé en INT pour correspondre à Commandes.Id
    CommandeId INT NOT NULL, 
    
    -- Contrainte de clé étrangère
    CONSTRAINT FK_Product_Commande 
        FOREIGN KEY (CommandeId) 
        REFERENCES Commande(Id) 
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- Index pour optimiser les jointures
CREATE INDEX idx_commande_id ON ProductCommande(CommandeId);
