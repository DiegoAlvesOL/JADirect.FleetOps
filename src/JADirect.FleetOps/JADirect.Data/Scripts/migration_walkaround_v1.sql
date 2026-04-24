DROP PROCEDURE IF EXISTS MigrateWalkaroundChecklist;

DELIMITER $$

CREATE PROCEDURE MigrateWalkaroundChecklist()
BEGIN
    DECLARE isDone INT DEFAULT 0;
    DECLARE currentRecordId INT;
    DECLARE currentJson TEXT;
    DECLARE newJson TEXT;

    DECLARE walkaroundCursor CURSOR FOR
SELECT id, checklist_json FROM walkaround_checks;

DECLARE CONTINUE HANDLER FOR NOT FOUND SET isDone = 1;

OPEN walkaroundCursor;

conversionLoop: LOOP
        FETCH walkaroundCursor INTO currentRecordId, currentJson;

        IF isDone THEN
            LEAVE conversionLoop;
END IF;

        SET newJson = '[]';

BEGIN
            DECLARE itemIndex INT DEFAULT 0;
            DECLARE totalItems INT;
            DECLARE itemKey VARCHAR(200);
            DECLARE itemValue VARCHAR(10);
            DECLARE itemState VARCHAR(20);
            DECLARE itemAction VARCHAR(20);

            SET totalItems = JSON_LENGTH(currentJson);

            WHILE itemIndex < totalItems DO
                SET itemKey = JSON_UNQUOTE(
                    JSON_EXTRACT(JSON_KEYS(currentJson),
                    CONCAT('$[', itemIndex, ']'))
                );

                SET itemValue = JSON_UNQUOTE(
                    JSON_EXTRACT(currentJson, CONCAT('$.', itemKey))
                );

                IF itemValue = 'Pass' THEN
                    SET itemState  = 'Good';
                    SET itemAction = 'None';
ELSE
                    SET itemState  = 'Defect';
                    SET itemAction = 'RequiresGarage';
END IF;

                SET newJson = JSON_ARRAY_APPEND(
                    newJson,
                    '$',
                    JSON_OBJECT(
                        'item',        itemKey,
                        'state',       itemState,
                        'actionTaken', itemAction,
                        'note',        NULL
                    )
                );

                SET itemIndex = itemIndex + 1;
END WHILE;
END;

UPDATE walkaround_checks
SET checklist_json = newJson
WHERE id = currentRecordId;

END LOOP;

CLOSE walkaroundCursor;
END$$

DELIMITER ;

CALL MigrateWalkaroundChecklist();

DROP PROCEDURE IF EXISTS MigrateWalkaroundChecklist;