-- =============================================
-- Скрипт обновления базы данных для модуля "Рецепты"
-- Согласно техническому заданию
-- =============================================

USE [MenuPlanner];
GO

-- =============================================
-- 1. Обновление таблицы Recipes
-- =============================================

-- Добавляем новые поля в таблицу Recipes согласно ТЗ
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'RecipeNumber')
    ALTER TABLE dbo.Recipes ADD RecipeNumber NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'Source')
    ALTER TABLE dbo.Recipes ADD Source NVARCHAR(255) NULL;
GO

-- Переиспользуем YieldPortions как BaseServings (базовое кол-во порций)
-- Если нужно, можно переименовать, но для совместимости оставляем старое имя
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'YieldPortions')
BEGIN
    -- Проверяем, есть ли уже BaseServings
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'BaseServings')
    BEGIN
        -- Создаем BaseServings и копируем данные из YieldPortions
        ALTER TABLE dbo.Recipes ADD BaseServings INT NULL;
        UPDATE dbo.Recipes SET BaseServings = YieldPortions WHERE YieldPortions IS NOT NULL;
    END
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'BaseServings')
        ALTER TABLE dbo.Recipes ADD BaseServings INT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'YieldWeight')
    ALTER TABLE dbo.Recipes ADD YieldWeight DECIMAL(18,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'Technology')
    ALTER TABLE dbo.Recipes ADD Technology NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'MarkupPercent')
    ALTER TABLE dbo.Recipes ADD MarkupPercent DECIMAL(18,2) NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recipes') AND name = 'IsActive')
    ALTER TABLE dbo.Recipes ADD IsActive BIT NULL DEFAULT 1;
GO

-- =============================================
-- 2. Обновление таблицы RecipeIngredients
-- =============================================

-- Добавляем новые поля в таблицу RecipeIngredients согласно ТЗ
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RecipeIngredients') AND name = 'GrossWeight')
    ALTER TABLE dbo.RecipeIngredients ADD GrossWeight DECIMAL(18,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RecipeIngredients') AND name = 'NetWeight')
    ALTER TABLE dbo.RecipeIngredients ADD NetWeight DECIMAL(18,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RecipeIngredients') AND name = 'SortOrder')
    ALTER TABLE dbo.RecipeIngredients ADD SortOrder INT NULL DEFAULT 0;
GO

-- Копируем данные из Quantity в GrossWeight для существующих записей
UPDATE dbo.RecipeIngredients 
SET GrossWeight = Quantity 
WHERE GrossWeight IS NULL AND Quantity IS NOT NULL;
GO

-- =============================================
-- 3. Тестовые данные
-- =============================================

DECLARE @recipeId1 INT, @recipeId2 INT, @ingredientId1 INT, @ingredientId2 INT, @ingredientId3 INT;

-- Получаем или создаем тестовые ингредиенты
IF NOT EXISTS (SELECT 1 FROM dbo.Ingredients WHERE Name = 'Картофель')
BEGIN
    INSERT INTO dbo.Ingredients (Name, Unit, ShelfLifeDays, DefaultPrice, IsActive, CategoryId)
    VALUES ('Картофель', 'кг', 30, 50.00, 1, NULL);
END
SET @ingredientId1 = (SELECT TOP 1 Id FROM dbo.Ingredients WHERE Name = 'Картофель');

IF NOT EXISTS (SELECT 1 FROM dbo.Ingredients WHERE Name = 'Молоко')
BEGIN
    INSERT INTO dbo.Ingredients (Name, Unit, ShelfLifeDays, DefaultPrice, IsActive, CategoryId)
    VALUES ('Молоко', 'л', 7, 80.00, 1, NULL);
END
SET @ingredientId2 = (SELECT TOP 1 Id FROM dbo.Ingredients WHERE Name = 'Молоко');

IF NOT EXISTS (SELECT 1 FROM dbo.Ingredients WHERE Name = 'Масло сливочное')
BEGIN
    INSERT INTO dbo.Ingredients (Name, Unit, ShelfLifeDays, DefaultPrice, IsActive, CategoryId)
    VALUES ('Масло сливочное', 'кг', 60, 600.00, 1, NULL);
END
SET @ingredientId3 = (SELECT TOP 1 Id FROM dbo.Ingredients WHERE Name = 'Масло сливочное');

-- Создаем тестовый рецепт 1: "Картофельное пюре"
IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Name = 'Картофельное пюре')
BEGIN
    INSERT INTO dbo.Recipes 
        (Name, RecipeNumber, Source, BaseServings, YieldWeight, Technology, MarkupPercent, IsActive, Category, Status, CreatedAt, UpdatedAt)
    VALUES 
        ('Картофельное пюре', '№1', 'Сборник рецептур 2007', 4, 1200, 
         '1. Картофель очистить и нарезать. 2. Отварить до готовности. 3. Добавить горячее молоко и масло. 4. Размять толкушкой.', 
         30.00, 1, 'Основные блюда', 'Активен', GETDATE(), GETDATE());
    
    SET @recipeId1 = SCOPE_IDENTITY();
    
    -- Добавляем ингредиенты для рецепта 1
    INSERT INTO dbo.RecipeIngredients (RecipeId, IngredientId, GrossWeight, NetWeight, Quantity, Unit, SortOrder)
    VALUES 
        (@recipeId1, @ingredientId1, 1000, 750, 1000, 'г', 1),
        (@recipeId1, @ingredientId2, 200, 200, 200, 'мл', 2),
        (@recipeId1, @ingredientId3, 50, 50, 50, 'г', 3);
END

-- Создаем тестовый рецепт 2: "Борщ московский"
IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Name = 'Борщ московский')
BEGIN
    INSERT INTO dbo.Recipes 
        (Name, RecipeNumber, Source, BaseServings, YieldWeight, Technology, MarkupPercent, IsActive, Category, Status, CreatedAt, UpdatedAt)
    VALUES 
        ('Борщ московский', '№2', 'Сборник рецептур 2007', 10, 5000, 
         '1. Сварить бульон. 2. Подготовить овощи. 3. Сделать зажарку. 4. Соединить все ингредиенты. 5. Варить до готовности.', 
         45.00, 1, 'Первые блюда', 'Активен', GETDATE(), GETDATE());
    
    SET @recipeId2 = SCOPE_IDENTITY();
    
    -- Добавляем ингредиенты для рецепта 2
    INSERT INTO dbo.RecipeIngredients (RecipeId, IngredientId, GrossWeight, NetWeight, Quantity, Unit, SortOrder)
    VALUES 
        (@recipeId2, @ingredientId1, 500, 400, 500, 'г', 1),
        (@recipeId2, @ingredientId3, 100, 100, 100, 'г', 2);
END

GO

-- =============================================
-- 4. Проверка результатов
-- =============================================

PRINT '=== Обновление базы данных завершено успешно! ===';
PRINT '';
PRINT 'Созданные рецепты:';
SELECT r.Id, r.Name, r.RecipeNumber, r.BaseServings, r.YieldWeight, r.MarkupPercent, r.Category
FROM dbo.Recipes r
WHERE r.Name IN ('Картофельное пюре', 'Борщ московский');

PRINT '';
PRINT 'Ингредиенты рецептов:';
SELECT ri.Id, r.Name as RecipeName, i.Name as IngredientName, 
       ri.GrossWeight as 'Брутто (г)', ri.NetWeight as 'Нетто (г)', ri.Quantity, ri.Unit
FROM dbo.RecipeIngredients ri
JOIN dbo.Recipes r ON ri.RecipeId = r.Id
JOIN dbo.Ingredients i ON ri.IngredientId = i.Id
WHERE r.Name IN ('Картофельное пюре', 'Борщ московский')
ORDER BY r.Name, ri.SortOrder;

GO
