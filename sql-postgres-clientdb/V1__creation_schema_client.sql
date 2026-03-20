CREATE SCHEMA microservice;


CREATE TABLE microservice.client ( 
  "id" SERIAL,
  "lastname" VARCHAR(100) NULL,
  "firstname" VARCHAR(100) NULL,
  "email" VARCHAR(100) NULL,
  "telephone" VARCHAR(50) NULL,
  "ville" VARCHAR(100) NULL,
  "codepostal" VARCHAR(20) NULL,
  "datenaissance" DATE NULL,
  "datecreation" DATE NULL,
  "datemodification" DATE NULL,
  CONSTRAINT "PK_client" PRIMARY KEY ("id")
);
