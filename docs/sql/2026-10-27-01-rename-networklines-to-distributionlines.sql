SET @rename_table = (
    SELECT IF(COUNT(*) > 0, 'RENAME TABLE NetworkLines TO DistributionLines', 'DO 0')
    FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = 'NetworkLines'
);
PREPARE rename_table FROM @rename_table;
EXECUTE rename_table;
DEALLOCATE PREPARE rename_table;

SET @rename_from = (
    SELECT IF(COUNT(*) > 0, 'ALTER TABLE DistributionLines RENAME INDEX IX_NetworkLines_FromNode TO IX_DistributionLines_FromNode', 'DO 0')
    FROM information_schema.statistics
    WHERE table_schema = DATABASE() AND table_name = 'DistributionLines' AND index_name = 'IX_NetworkLines_FromNode'
);
PREPARE rename_from FROM @rename_from;
EXECUTE rename_from;
DEALLOCATE PREPARE rename_from;

SET @rename_to = (
    SELECT IF(COUNT(*) > 0, 'ALTER TABLE DistributionLines RENAME INDEX IX_NetworkLines_ToNode TO IX_DistributionLines_ToNode', 'DO 0')
    FROM information_schema.statistics
    WHERE table_schema = DATABASE() AND table_name = 'DistributionLines' AND index_name = 'IX_NetworkLines_ToNode'
);
PREPARE rename_to FROM @rename_to;
EXECUTE rename_to;
DEALLOCATE PREPARE rename_to;

INSERT INTO SchemaScripts (Name) VALUES ('2026-10-27-01-rename-networklines-to-distributionlines');
