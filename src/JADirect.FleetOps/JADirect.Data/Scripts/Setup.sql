-- Criando o banco de dados
CREATE DATABASE IF NOT EXISTS ja_direct_db;
USE ja_direct_db;
    
-- Tabela de usuário
CREATE TABLE users(
    id INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    surname VARCHAR(100) NOT NULL ,
    email VARCHAR(150) NOT NULL,
    phone_number VARCHAR(20) NOT NULL,
    password_hash VARCHAR (255) NOT NULL,
    role_id INT NOT NULL,
    status_id INT NOT NULL,
    created_at DATETIME NOT NULL,
    INDEX idx_user_email (email)
);

-- Tabela de Veículos
CREATE TABLE vehicles(
    id INT AUTO_INCREMENT PRIMARY KEY,
    registration_no VARCHAR(20) NOT NULL UNIQUE,
    manufacturer VARCHAR(50) NOT NULL,
    model VARCHAR(50) NOT NULL,
    vehicle_type_id INT NOT NULL,
    current_km INT NOT NULL DEFAULT 0,
    status_id INT NOT NULL,
    created_at DATETIME NOT NULL,
    INDEX idx_vehicle_reg (registration_no)
);

-- Tabela de Inspeções (Walkaround Checks)
CREATE TABLE walkaround_checks(
    id INT AUTO_INCREMENT PRIMARY KEY,
    check_date DATETIME NOT NULL,
    user_id INT NOT NULL,
    vehicle_id INT NOT NULL,
    odometer INT NOT NULL CHECK (odometer >=0),
    checklist_json TEXT NOT NULL,
    has_defect TINYINT(1) NOT NULL DEFAULT 0,
    defect_notes TEXT,
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(id),
    INDEX idx_check_date (check_date)
);

-- Tabela de Daily Logs
CREATE TABLE daily_logs(
    id INT AUTO_INCREMENT PRIMARY KEY,
    log_date DATETIME NOT NULL,
    user_id INT NOT NULL,
    vehicle_id INT NOT NULL,
    deliveries INT NOT NULL DEFAULT 0,
    collections INT NOT NULL DEFAULT 0,
    returns INT NOT NULL DEFAULT 0,
    notes TEXT,
    created_at DATETIME NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
);