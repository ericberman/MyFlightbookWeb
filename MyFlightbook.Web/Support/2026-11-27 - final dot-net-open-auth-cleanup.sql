drop table nonce;
drop table pkceauthorization;
DELETE FROM cryptokeys WHERE bucket <> 'https://localhost/dnoa/oauth_refresh_token';