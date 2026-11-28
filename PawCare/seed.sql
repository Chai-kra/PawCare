-- Run ONCE after Update-Database (SSMS / VS SQL Server Object Explorer, on the PawCare LocalDB).
INSERT INTO Categories (Id, Name) VALUES
 ('CAT1','Baths and spa'),('CAT2','Cuts and styling'),('CAT3','Hygiene and health'),
 ('PRD1','Food'),('PRD2','Toys');

INSERT INTO Services (Id, Name, Description, Price, Duration, CategoryId) VALUES
 ('S000001','Basic bath','Shampoo, rinse and blow dry.',45.00,60,'CAT1'),
 ('S000002','Spa bath','Conditioning treatment and fluffy blow dry.',70.00,90,'CAT1'),
 ('S000003','Full groom','Bath, haircut and styling.',90.00,120,'CAT2'),
 ('S000004','Nail trim','Quick nail trim and file.',15.00,30,'CAT3'),
 ('S000005','Ear cleaning','Gentle ear clean.',20.00,30,'CAT3');

INSERT INTO Products (Id, Name, Description, Price, Stock, CategoryId) VALUES
 ('P000001','Dry dog food 2kg','Complete adult dog food.',39.90,20,'PRD1'),
 ('P000002','Cat treats','Crunchy chicken treats.',9.90,50,'PRD1'),
 ('P000003','Rope toy','Durable chew toy.',12.50,30,'PRD2');

INSERT INTO ServicePackages (Id, ServiceType, Price, TimeAmount) VALUES
 ('SP00001','Basic bath',45.00,60),
 ('SP00002','Spa bath',70.00,90),
 ('SP00003','Full groom',90.00,120),
 ('SP00004','Nail trim',15.00,30);

INSERT INTO Staff (Id, Name, Email, Phone, Role) VALUES
 ('ST00001','Sarah Lim','sarah@pawcare.example','012-3456789','Staff'),
 ('ST00002','Daniel Tan','daniel@pawcare.example','012-3456780','Staff'),
 ('ST00003','Admin','admin@pawcare.example',NULL,'Admin');
