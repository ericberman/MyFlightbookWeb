ALTER TABLE Users ADD COLUMN search_text VARCHAR(800)
    CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci
    GENERATED ALWAYS AS (
        CONCAT_WS(' ', username, firstName, lastName, email)
    ) STORED;
ALTER TABLE Users ADD FULLTEXT INDEX ft_user_search (search_text);
