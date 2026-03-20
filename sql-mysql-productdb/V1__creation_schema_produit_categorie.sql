CREATE TABLE `categorie` ( 
  `id` INT AUTO_INCREMENT NOT NULL,
  `name` VARCHAR(100) NOT NULL,
   PRIMARY KEY (`id`)
)
ENGINE = InnoDB
COMMENT = 'table des catégories';

CREATE TABLE `product` (
  `id` BINARY(16) NOT NULL,
  `name` VARCHAR(250) NULL,
  `description` VARCHAR(250) NULL,
  `prix` DECIMAL(10,0) NULL,
  `qtestock` INT NOT NULL,
  `datecreation` DATETIME NOT NULL,
  `datemodification` DATETIME NOT NULL,
  `actif` TINYINT NULL,
  `idcategorie` INT NOT NULL,
  PRIMARY KEY (`id`),
  CONSTRAINT `product_categorie` FOREIGN KEY (`idcategorie`) REFERENCES `categorie` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB
COMMENT = 'table des produits';

CREATE INDEX `idx_product_categorie` ON `product` (`idcategorie` ASC);
