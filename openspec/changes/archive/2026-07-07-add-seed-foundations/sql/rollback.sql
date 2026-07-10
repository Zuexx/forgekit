-- Rollback script for add-seed-foundations seeding
-- Deletes records matching the seed patterns inserted by PocDataSeeder.
-- Run within the development database (local_qrmp) with caution.
BEGIN TRANSACTION;
DELETE FROM complianceAudit WHERE auditType='Seed' OR performedBy='seed' OR actionDescription='Seeded';
DELETE FROM rmpInformation WHERE documentVersion LIKE 'rmp-%';
DELETE FROM visitStatusHistory WHERE notes='Seeded' OR status='Requested';
DELETE FROM materialDistribution WHERE notes='Seeded';
DELETE FROM educationalProgress WHERE status IN ('Completed','InProgress');
DELETE FROM visitRequest WHERE purpose='Seed visit' OR notes='Seeded';
DELETE FROM monthlyAnalyticsSnapshot WHERE regionId IN (SELECT id FROM region WHERE regionName LIKE 'Region %');
DELETE FROM regionalAnalytics WHERE regionName LIKE 'Region %';
DELETE FROM educationalMaterial WHERE title LIKE 'Material %';
DELETE FROM healthcareProfessional WHERE licenseNumber LIKE 'LIC-%' OR email LIKE '%@example.test';
DELETE FROM healthcareOrganization WHERE name LIKE 'HCO %';
DELETE FROM medicalRepresentative WHERE employeeId LIKE 'EMP-%' OR email LIKE '%@example.test';
DELETE FROM territoryRegion WHERE territoryId IN (SELECT id FROM territory WHERE territoryCode LIKE 'T-%');
DELETE FROM territory WHERE territoryCode LIKE 'T-%';
DELETE FROM region WHERE regionName LIKE 'Region %';
COMMIT;
