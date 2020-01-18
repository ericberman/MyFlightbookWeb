/* 1) Execute this query to find the flights to manually remove the existing approach types & fix up the flight properties (about 10 rows).  For these, set the approach types to 0 and add
      the relevant properties to make it whole (using impersonation).
*/
SELECT
 f.idFlight, f.username, f.cApprPrecision, f.cApprNonPrecision,
 CAST(DATE_FORMAT(f.date, '%m/%d/%Y') AS CHAR) AS DateDisplay,
 flightprops.properties AS CustomProperties
FROM flights f
           LEFT OUTER JOIN (SELECT GROUP_CONCAT(DISTINCT REPLACE(cpt.FormatString, '{0}',
                    ELT(cpt.type + 1, cast(fdc.intValue as char), cast(FORMAT(fdc.decValue, 1) as char), if(fdc.intValue = 0, 'No', 'Yes'),
                    DATE_FORMAT(fdc.DateValue, '%m/%d/%Y'),
                    cast(DateValue as char),
                    StringValue,
                    cast(fdc.decValue AS char))) separator ', ')  AS properties,
                fdc.idFlight AS idFlightForProperty
                FROM flightproperties fdc INNER JOIN custompropertytypes cpt ON fdc.idPropType=cpt.idPropType
                GROUP BY fdc.idFlight) flightprops ON f.idFlight=flightprops.idFlightForProperty
WHERE f.cApprPrecision + f.cApprNonPrecision > 0

/* 2) Now re-execute the query - should not have any properties for approach types with flights that have counts of approach types. */

/* 3) Run the following two queries to create generic approach properties */
insert into flightproperties (idFlight, idPropType, DecValue, IntValue, DateValue, StringValue)
select idflight, 78 as idPropType, 0.0 as DecValue, cApprPrecision as IntValue, '0001-01-01 00:00:00' AS DateValue, '' AS StringValue
from flights where flights.cApprPrecision > 0;

insert into flightproperties (idFlight, idPropType, DecValue, IntValue, DateValue, StringValue)
select idflight, 79 as idPropType, 0.0 as DecValue, cApprNonPrecision as IntValue, '0001-01-01 00:00:00' AS DateValue, '' AS StringValue
from flights where flights.cApprNonPrecision > 0;

/* 4) Run the query again, to verify that things match up */

/* 5) Clear the approach types like this: */
update flights set cApprPrecision=0, cApprNonPrecision=0 WHERE cApprPrecision+cApprNonPrecision > 0

/* 6) Sync to the new UI which removes the approach type fields */
