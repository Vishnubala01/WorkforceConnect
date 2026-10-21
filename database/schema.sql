-- WorkforceConnect Portal — MySQL 8 schema (Phase 1: role-based foundation)
--
-- Two ways to create the database:
--   A) Let the API create it on first run (Database:AutoCreate = true, the default in Development), or
--   B) Run this script, then start the API with Database:AutoCreate = false.
--
-- Reference data (permissions, roles, role-permission grants, organizations, teams and the first
-- Super Admin) is NOT inserted here. The API seeds it at startup (DbSeeder), idempotently,
-- so there is one source of truth for it.

CREATE DATABASE IF NOT EXISTS workforceconnect
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE workforceconnect;

-- ---------------------------------------------------------------------------
-- Roles and permissions
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Roles (
  Id          INT           NOT NULL AUTO_INCREMENT,
  Name        VARCHAR(50)   NOT NULL,
  Description VARCHAR(250)  NULL,
  -- Data scope level: All | Organization | Team | Self (what data the role may reach)
  DataScope   VARCHAR(20)   NOT NULL,
  IsSystem    TINYINT(1)    NOT NULL DEFAULT 0,
  CreatedAt   DATETIME(6)   NOT NULL,
  UpdatedAt   DATETIME(6)   NOT NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UX_Roles_Name (Name)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Permissions (
  Id          INT           NOT NULL AUTO_INCREMENT,
  Name        VARCHAR(100)  NOT NULL,          -- e.g. Organization.Create
  Module      VARCHAR(50)   NOT NULL,          -- e.g. Organization
  Description VARCHAR(250)  NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UX_Permissions_Name (Name)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS RolePermissions (
  RoleId       INT NOT NULL,
  PermissionId INT NOT NULL,
  PRIMARY KEY (RoleId, PermissionId),
  KEY IX_RolePermissions_PermissionId (PermissionId),
  CONSTRAINT FK_RolePermissions_Roles
    FOREIGN KEY (RoleId) REFERENCES Roles (Id) ON DELETE CASCADE,
  CONSTRAINT FK_RolePermissions_Permissions
    FOREIGN KEY (PermissionId) REFERENCES Permissions (Id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ---------------------------------------------------------------------------
-- Organization structure
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Organizations (
  Id          INT           NOT NULL AUTO_INCREMENT,
  Name        VARCHAR(100)  NOT NULL,
  Description VARCHAR(500)  NULL,
  IsActive    TINYINT(1)    NOT NULL DEFAULT 1,
  CreatedAt   DATETIME(6)   NOT NULL,
  UpdatedAt   DATETIME(6)   NOT NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UX_Organizations_Name (Name)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Teams (
  Id             INT           NOT NULL AUTO_INCREMENT,
  OrganizationId INT           NOT NULL,
  Name           VARCHAR(100)  NOT NULL,
  Description    VARCHAR(500)  NULL,
  IsActive       TINYINT(1)    NOT NULL DEFAULT 1,
  CreatedAt      DATETIME(6)   NOT NULL,
  UpdatedAt      DATETIME(6)   NOT NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UX_Teams_Organization_Name (OrganizationId, Name),
  CONSTRAINT FK_Teams_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Employees (
  Id             INT           NOT NULL AUTO_INCREMENT,
  EmployeeCode   VARCHAR(30)   NOT NULL,
  FirstName      VARCHAR(100)  NOT NULL,
  LastName       VARCHAR(100)  NOT NULL,
  Email          VARCHAR(256)  NOT NULL,
  OrganizationId INT           NOT NULL,
  TeamId         INT           NULL,
  IsActive       TINYINT(1)    NOT NULL DEFAULT 1,
  CreatedAt      DATETIME(6)   NOT NULL,
  UpdatedAt      DATETIME(6)   NOT NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UX_Employees_EmployeeCode (EmployeeCode),
  UNIQUE KEY UX_Employees_Email (Email),
  KEY IX_Employees_OrganizationId (OrganizationId),
  KEY IX_Employees_TeamId (TeamId),
  CONSTRAINT FK_Employees_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE RESTRICT,
  CONSTRAINT FK_Employees_Teams
    FOREIGN KEY (TeamId) REFERENCES Teams (Id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- ---------------------------------------------------------------------------
-- Users and data-scope assignments
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Users (
  Id           INT           NOT NULL AUTO_INCREMENT,
  EmployeeId   INT           NULL,
  Email        VARCHAR(256)  NOT NULL,
  PasswordHash VARCHAR(100)  NOT NULL,           -- BCrypt
  RoleId       INT           NOT NULL,
  IsActive     TINYINT(1)    NOT NULL DEFAULT 1,
  LastLoginAt  DATETIME(6)   NULL,
  CreatedAt    DATETIME(6)   NOT NULL,
  UpdatedAt    DATETIME(6)   NOT NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UX_Users_Email (Email),
  UNIQUE KEY UX_Users_EmployeeId (EmployeeId),
  KEY IX_Users_RoleId (RoleId),
  CONSTRAINT FK_Users_Roles
    FOREIGN KEY (RoleId) REFERENCES Roles (Id) ON DELETE RESTRICT,
  CONSTRAINT FK_Users_Employees
    FOREIGN KEY (EmployeeId) REFERENCES Employees (Id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- Organizations a Manager-level user is assigned to
CREATE TABLE IF NOT EXISTS UserOrganizationAccess (
  UserId         INT NOT NULL,
  OrganizationId INT NOT NULL,
  PRIMARY KEY (UserId, OrganizationId),
  KEY IX_UserOrganizationAccess_OrganizationId (OrganizationId),
  CONSTRAINT FK_UserOrganizationAccess_Users
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
  CONSTRAINT FK_UserOrganizationAccess_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- Teams a TeamLead-level user (or a Manager with extra teams) is assigned to
CREATE TABLE IF NOT EXISTS UserTeamAccess (
  UserId INT NOT NULL,
  TeamId INT NOT NULL,
  PRIMARY KEY (UserId, TeamId),
  KEY IX_UserTeamAccess_TeamId (TeamId),
  CONSTRAINT FK_UserTeamAccess_Users
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
  CONSTRAINT FK_UserTeamAccess_Teams
    FOREIGN KEY (TeamId) REFERENCES Teams (Id) ON DELETE CASCADE
) ENGINE=InnoDB;
