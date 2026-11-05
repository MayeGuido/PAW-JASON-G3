-- Fuentes de ejemplo para probar la app. Solo inserta las que todavía no existen.
-- Ojo: estas direcciones salen de nuestra lista de candidatas y todavía hay que comprobar
-- que respondan hoy y si piden API key.

USE JasonDb;
GO

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://api.spaceflightnewsapi.net/v4/articles')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://api.spaceflightnewsapi.net/v4/articles', 'Spaceflight News', 'Noticias del espacio (JSON)', 'api', 0);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://feeds.bbci.co.uk/news/rss.xml')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://feeds.bbci.co.uk/news/rss.xml', 'BBC News', 'Noticias generales (RSS, XML)', 'feed', 0);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://hacker-news.firebaseio.com/v0/topstories.json')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://hacker-news.firebaseio.com/v0/topstories.json', 'Hacker News', 'Noticias de tecnología (JSON)', 'api', 0);
GO
