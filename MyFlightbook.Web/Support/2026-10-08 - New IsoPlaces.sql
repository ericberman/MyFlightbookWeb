insert into localconfig set keyname='GoogleMapIDVisited', keyvalue='';
CREATE TABLE isoplaceids (
  isokey  VARCHAR(100) NOT NULL PRIMARY KEY,
  placeid VARCHAR(255) NOT NULL
) DEFAULT CHARSET=utf8mb4;
