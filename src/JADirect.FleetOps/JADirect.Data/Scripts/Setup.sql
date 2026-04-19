-- Criando o banco de dados
CREATE DATABASE IF NOT EXISTS ja_direct_db;
USE ja_direct_db;

-- Tabela de usuário
CREATE TABLE users(
                      id INT AUTO_INCREMENT PRIMARY KEY,
                      first_name VARCHAR(100) NOT NULL,
                      surname VARCHAR(100) NOT NULL,
                      email VARCHAR(150) NOT NULL,
                      phone_number VARCHAR(20) NOT NULL,
                      password_hash VARCHAR(255) NOT NULL,
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
                         last_walkaround_at DATETIME NULL,
                         INDEX idx_vehicle_reg (registration_no),
                         INDEX idx_vehicle_last_check (last_walkaround_at)
);

-- Tabela de Inspeções (Walkaround Checks)
CREATE TABLE walkaround_checks(
                                  id INT AUTO_INCREMENT PRIMARY KEY,
                                  check_date DATETIME NOT NULL,
                                  user_id INT NOT NULL,
                                  vehicle_id INT NOT NULL,
                                  odometer INT NOT NULL CHECK (odometer >= 0),
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
                           current_odometer INT NULL,
                           notes TEXT,
                           created_at DATETIME NOT NULL,
                           FOREIGN KEY (user_id) REFERENCES users(id),
                           FOREIGN KEY (vehicle_id) REFERENCES vehicles(id),
    -- Índice simples para cobrir a foreign key de vehicle_id
                           INDEX idx_daily_logs_vehicle_id (vehicle_id),
    -- Regra de unicidade: um motorista só pode ter um log por dia,
    -- independente do veículo utilizado.
                           CONSTRAINT uq_daily_log_user_date UNIQUE (user_id, log_date)
);


-- Tabela de configuração dos itens do checklist por tipo de veículo.
-- É uma tabela de configuração, não transacional.
-- Dados aqui definem o comportamento do formulário sem necessidade de deploy.
CREATE TABLE checklist_items (
                                 id              INT AUTO_INCREMENT PRIMARY KEY,
                                 tenant_id       INT NOT NULL DEFAULT 1,
                                 vehicle_type_id INT NOT NULL,
                                 category        VARCHAR(100) NOT NULL,
                                 label           VARCHAR(200) NOT NULL,
                                 sort_order      INT NOT NULL DEFAULT 0,
                                 is_active       TINYINT(1) NOT NULL DEFAULT 1,
                                 created_at      DATETIME NOT NULL DEFAULT NOW(),
                                 INDEX idx_checklist_tenant_type (tenant_id, vehicle_type_id)
);


-- Van (vehicle_type_id = 1) — 18 itens
-- Corresponde ao checklist atual hardcoded no Create.cshtml
INSERT INTO checklist_items (tenant_id, vehicle_type_id, category, label, sort_order) 
VALUES
                                                                                          
    (1, 1, 'Exterior & Structure', 'Tyres condition, inflation & fixings', 1),
    (1, 1, 'Exterior & Structure', 'Lights, indicators & reflectors', 2),
    (1, 1, 'Exterior & Structure', 'CVRT tax & insurance discs', 3),
    (1, 1, 'Exterior & Structure', 'Bumpers & sideguards', 4),
    (1, 1, 'Exterior & Structure', 'Number plates & marker plates', 5),
    (1, 1, 'Exterior & Structure', 'Mirrors & windows', 6),
    (1, 1, 'Exterior & Structure', 'Fuel cap secure', 7),
    (1, 1, 'Engine & Fluids', 'Oil, water, washer & fuel levels', 8),
    (1, 1, 'Engine & Fluids', 'Fuel / oil leaks', 9),
    (1, 1, 'Engine & Fluids', 'Exhaust & smoke emission', 10),
    (1, 1, 'Engine & Fluids', 'Wiring & battery', 11),
    (1, 1, 'Cab & Controls', 'Windscreen wipers & washers', 12),
    (1, 1, 'Cab & Controls', 'Horn', 13),
    (1, 1, 'Cab & Controls', 'Drivers seat & seat belts', 14),
    (1, 1, 'Cab & Controls', 'Steering controls & operation', 15),
    (1, 1, 'Cab & Controls', 'Brake controls & operation', 16),
    (1, 1, 'Cab & Controls', 'ABS/EBS & instruments/gauges', 17),
    (1, 1, 'Cab & Controls', 'Load security & distribution', 18);


-- RigidTruck (vehicle_type_id = 2) — 25 itens
-- Baseado no formulário oficial Dublin City Council, excluindo seção articulada
INSERT INTO checklist_items (tenant_id, vehicle_type_id, category, label, sort_order) 
VALUES
    (1, 2, 'In-Cab Items', 'Mirrors', 1),
    (1, 2, 'In-Cab Items', 'Windows', 2),
    (1, 2, 'In-Cab Items', 'Driving controls', 3),
    (1, 2, 'In-Cab Items', 'Safety belts', 4),
    (1, 2, 'In-Cab Items', 'Windscreen washers and wipers', 5),
    (1, 2, 'In-Cab Items', 'Horn', 6),
    (1, 2, 'In-Cab Items', 'Tachograph', 7),
    (1, 2, 'In-Cab Items', 'ABS and EBS warning lights', 8),
    (1, 2, 'In-Cab Items', 'Instruments, gauges and warning devices', 9),
    (1, 2, 'In-Cab Items', 'Air leaks and pressure', 10),
    (1, 2, 'External Vehicle Checks', 'CRW, tax, insurance disc & driving licence', 11),
    (1, 2, 'External Vehicle Checks', 'Tyres', 12),
    (1, 2, 'External Vehicle Checks', 'Wheel condition and security', 13),
    (1, 2, 'External Vehicle Checks', 'All lights and reflectors', 14),
    (1, 2, 'External Vehicle Checks', 'Exhaust', 15),
    (1, 2, 'External Vehicle Checks', 'Steps', 16),
    (1, 2, 'External Vehicle Checks', 'Vehicle body, doors and curtains', 17),
    (1, 2, 'External Vehicle Checks', 'Number plates and marker plates', 18),
    (1, 2, 'External Vehicle Checks', 'Fuel level and leaks', 19),
    (1, 2, 'External Vehicle Checks', 'Engine oil', 20),
    (1, 2, 'External Vehicle Checks', 'Coolant, washer bottle and other levels', 21),
    (1, 2, 'External Vehicle Checks', 'Load security and weight distribution', 22),
    (1, 2, 'External Vehicle Checks', 'Air suspension', 23),
    (1, 2, 'Checks With Engine Started', 'Steering', 24),
    (1, 2, 'Checks With Engine Started', 'Brake operation', 25);



-- ArticulatedTruck (vehicle_type_id = 3) — 30 itens
-- Inclui todos os 25 itens do RigidTruck mais os 5 itens exclusivos do reboque
INSERT INTO checklist_items (tenant_id, vehicle_type_id, category, label, sort_order) 
VALUES
    (1, 3, 'In-Cab Items', 'Mirrors', 1),
    (1, 3, 'In-Cab Items', 'Windows', 2),
    (1, 3, 'In-Cab Items', 'Driving controls', 3),
    (1, 3, 'In-Cab Items', 'Safety belts', 4),
    (1, 3, 'In-Cab Items', 'Windscreen washers and wipers', 5),
    (1, 3, 'In-Cab Items', 'Horn', 6),
    (1, 3, 'In-Cab Items', 'Tachograph', 7),
    (1, 3, 'In-Cab Items', 'ABS and EBS warning lights', 8),
    (1, 3, 'In-Cab Items', 'Instruments, gauges and warning devices', 9),
    (1, 3, 'In-Cab Items', 'Air leaks and pressure', 10),
    (1, 3, 'External Vehicle Checks', 'CRW, tax, insurance disc & driving licence', 11),
    (1, 3, 'External Vehicle Checks', 'Tyres', 12),
    (1, 3, 'External Vehicle Checks', 'Wheel condition and security', 13),
    (1, 3, 'External Vehicle Checks', 'All lights and reflectors', 14),
    (1, 3, 'External Vehicle Checks', 'Exhaust', 15),
    (1, 3, 'External Vehicle Checks', 'Steps', 16),
    (1, 3, 'External Vehicle Checks', 'Vehicle body, doors and curtains', 17),
    (1, 3, 'External Vehicle Checks', 'Number plates and marker plates', 18),
    (1, 3, 'External Vehicle Checks', 'Fuel level and leaks', 19),
    (1, 3, 'External Vehicle Checks', 'Engine oil', 20),
    (1, 3, 'External Vehicle Checks', 'Coolant, washer bottle and other levels', 21),
    (1, 3, 'External Vehicle Checks', 'Load security and weight distribution', 22),
    (1, 3, 'External Vehicle Checks', 'Air suspension', 23),
    (1, 3, 'Articulated & Trailer Checks', 'Susie connections', 24),
    (1, 3, 'Articulated & Trailer Checks', 'Fifth wheel and locking devices', 25),
    (1, 3, 'Articulated & Trailer Checks', 'Coupling / tow bar', 26),
    (1, 3, 'Articulated & Trailer Checks', 'Landing legs and handle', 27),
    (1, 3, 'Articulated & Trailer Checks', 'Trailer park brake', 28),
    (1, 3, 'Checks With Engine Started', 'Steering', 29),
    (1, 3, 'Checks With Engine Started', 'Brake operation', 30);


-- Verificação 1: total por tipo de veículo
SELECT vehicle_type_id, COUNT(*) as total
FROM checklist_items
GROUP BY vehicle_type_id;
-- Esperado: 1=18, 2=25, 3=30

-- Verificação 2: total geral
SELECT COUNT(*) as total_geral FROM checklist_items;
-- Esperado: 73

-- Verificação 3: ordenação da Van está correta
SELECT sort_order, category, label
FROM checklist_items
WHERE vehicle_type_id = 1
ORDER BY sort_order;
-- Esperado: 18 itens em ordem crescente, agrupados por categoria


