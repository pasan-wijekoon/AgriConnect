CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "FullName" character varying(100) NOT NULL,
    "Email" character varying(150) NOT NULL,
    "PasswordHash" character varying(255) NOT NULL,
    "Role" character varying(20) NOT NULL,
    "Phone" character varying(20),
    "Region" character varying(100),
    "AvatarUrl" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");

-- Insert demo users if not existing
-- Password for all: "password" (PBKDF2-HMAC-SHA256, see AuthService.HashPassword)
INSERT INTO "Users" ("Id", "FullName", "Email", "PasswordHash", "Role", "Phone", "Region", "AvatarUrl", "CreatedAt", "IsActive")
VALUES
('f0000000-0000-0000-0000-000000000001', 'Kamal Perera', 'farmer@agriconnect.lk', '600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=', 'Farmer', '+94771234567', 'Nuwara Eliya', NULL, NOW(), true),
('f0000000-0000-0000-0000-000000000002', 'Saman Silva', 'farmer2@agriconnect.lk', '600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=', 'Farmer', '+94779876543', 'Kandy', NULL, NOW(), true),
('f0000000-0000-0000-0000-000000000010', 'Nihal Fernando', 'buyer@agriconnect.lk', '600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=', 'Buyer', '+94701234567', 'Colombo', NULL, NOW(), true),
('f0000000-0000-0000-0000-000000000099', 'N. Perera', 'admin@agriconnect.lk', '600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=', 'Admin', '+94112345678', 'Nuwara Eliya', NULL, NOW(), true)
ON CONFLICT ("Email") DO NOTHING;
