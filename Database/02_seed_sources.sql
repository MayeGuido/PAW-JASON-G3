-- Fuentes reales para probar los tres parsers. Solo inserta las que todavía no existen.

USE JasonDb;
GO

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://api.spaceflightnewsapi.net/v4/articles')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://api.spaceflightnewsapi.net/v4/articles', 'Spaceflight News', 'Noticias del espacio (JSON)', 'api', 0);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://feeds.bbci.co.uk/news/rss.xml')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://feeds.bbci.co.uk/news/rss.xml', 'BBC News', 'Noticias generales (RSS, XML)', 'feed', 0);

-- El seed inicial usaba la lista de ids de Firebase. La página HTML permite probar el parser completo.
UPDATE Sources
SET Url = 'https://news.ycombinator.com/',
    Description = 'Noticias de tecnología (HTML)',
    ComponentType = 'widget',
    RequiresSecret = 0
WHERE Url = 'https://hacker-news.firebaseio.com/v0/topstories.json';

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://news.ycombinator.com/')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://news.ycombinator.com/', 'Hacker News', 'Noticias de tecnología (HTML)', 'widget', 0);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/significant_month.geojson')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/significant_month.geojson', 'USGS Earthquakes', 'Sismos significativos del último mes (JSON)', 'api', 0);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://openlibrary.org/search.json?q=programming&limit=20')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://openlibrary.org/search.json?q=programming&limit=20', 'Open Library', 'Libros de programación (JSON)', 'api', 0);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://api.nasa.gov/planetary/apod')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://api.nasa.gov/planetary/apod', 'NASA APOD', 'Imagen astronómica del día (JSON, requiere API key)', 'api', 1);

IF NOT EXISTS (SELECT 1 FROM Sources WHERE Url = 'https://export.arxiv.org/api/query?search_query=all:computer&start=0&max_results=20')
    INSERT INTO Sources (Url, Name, Description, ComponentType, RequiresSecret)
    VALUES ('https://export.arxiv.org/api/query?search_query=all:computer&start=0&max_results=20', 'arXiv', 'Artículos científicos de computación (Atom XML)', 'feed', 0);

-- La N indica a SQL Server que estos textos son Unicode y conserva correctamente las tildes.
UPDATE Sources SET Name = N'Spaceflight News', Description = N'Noticias del espacio (JSON)'
WHERE Url = 'https://api.spaceflightnewsapi.net/v4/articles';

UPDATE Sources SET Name = N'BBC News', Description = N'Noticias generales (RSS, XML)'
WHERE Url = 'https://feeds.bbci.co.uk/news/rss.xml';

UPDATE Sources SET Name = N'Hacker News', Description = N'Noticias de tecnología (HTML)'
WHERE Url = 'https://news.ycombinator.com/';

UPDATE Sources SET Name = N'USGS Earthquakes', Description = N'Sismos significativos del último mes (JSON)'
WHERE Url = 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/significant_month.geojson';

UPDATE Sources SET Name = N'Open Library', Description = N'Libros de programación (JSON)'
WHERE Url = 'https://openlibrary.org/search.json?q=programming&limit=20';

UPDATE Sources SET Name = N'NASA APOD', Description = N'Imagen astronómica del día (JSON, requiere API key)'
WHERE Url = 'https://api.nasa.gov/planetary/apod';

UPDATE Sources SET Name = N'arXiv', Description = N'Artículos científicos de computación (Atom XML)'
WHERE Url = 'https://export.arxiv.org/api/query?search_query=all:computer&start=0&max_results=20';
GO
