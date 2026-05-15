// tests/products.spec.js
const { test, expect } = require('@playwright/test');
const { ProductsPage } = require('../pages/ProductsPage');

test.describe('Продукты - UI тесты', () => {
    let productsPage;

    test.beforeEach(async ({ page }) => {
        productsPage = new ProductsPage(page);
        await productsPage.goto();
    });

    test.describe('Эквивалентное разбиение', () => {
        
        test('Создание продукта с корректными данными', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Тестовый продукт');
            await productsPage.fillCalories(100);
            await productsPage.fillProteins(10);
            await productsPage.fillFats(5);
            await productsPage.fillCarbs(20);
            await productsPage.selectCategory('Овощи');
            await productsPage.clickSave();

            await expect(productsPage.getProductCardByText('Тестовый продукт')).toBeVisible();
        });

        test('Создание продукта с пустым названием - ошибка', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('');
            
            const responsePromise = productsPage.page.waitForResponse(
                response => response.url().includes('/api/Products')
            );
            
            await productsPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
            
            const errorText = await response.text();
            expect(errorText).toContain('Название продукта обязательно');
        });
    });

    test.describe('Анализ граничных значений (целые числа)', () => {
        
        test('Название из 2 символов (мин. длина)', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('АБ');
            await productsPage.clickSave();

            await expect(productsPage.getProductCardByText('АБ')).toBeVisible();
        });

        test('Название из 1 символа (ниже мин. длины) - ошибка', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('А');
            
            const responsePromise = productsPage.page.waitForResponse(
                response => response.url().includes('/api/Products')
            );
            
            await productsPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
            
            const errorText = await response.text();
            expect(errorText).toContain('минимум 2 символа');
        });

        test('Сумма БЖУ = 100 (максимально допустимая)', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Максимальный БЖУ');
            await productsPage.fillProteins(50);
            await productsPage.fillFats(30);
            await productsPage.fillCarbs(20);
            await productsPage.selectCategory('Овощи');
            await productsPage.clickSave();

            await expect(productsPage.getProductCardByText('Максимальный БЖУ')).toBeVisible();
        });

        test('Сумма БЖУ = 101 (выше лимита) - ошибка', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Превышающий БЖУ');
            await productsPage.fillProteins(51);
            await productsPage.fillFats(30);
            await productsPage.fillCarbs(20);
            await productsPage.selectCategory('Овощи');
            
            const responsePromise = productsPage.page.waitForResponse(
                response => response.url().includes('/api/Products')
            );
            
            await productsPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
            
            const error = await response.text();
            expect(error).toContain('Сумма БЖУ на 100г не может превышать 100');
        });
    });

    test.describe('Анализ граничных значений (float)', () => {
        
        test('Сумма БЖУ = 100.0 (ровно лимит)', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Максимальный БЖУ точный');
            await productsPage.fillProteins('50.0');
            await productsPage.fillFats('30.0');
            await productsPage.fillCarbs('20.0');
            await productsPage.selectCategory('Овощи');
            await productsPage.clickSave();

            await expect(productsPage.getProductCardByText('Максимальный БЖУ точный')).toBeVisible();
        });

        test('Сумма БЖУ = 100.1 (чуть выше лимита) - ошибка', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Чуть выше лимита');
            await productsPage.fillProteins('50.1');
            await productsPage.fillFats('30.0');
            await productsPage.fillCarbs('20.0');
            await productsPage.selectCategory('Овощи');
            
            const responsePromise = productsPage.page.waitForResponse(
                response => response.url().includes('/api/Products')
            );
            
            await productsPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
            
            const error = await response.text();
            expect(error).toContain('Сумма БЖУ на 100г не может превышать 100');
        });

        test('Сумма БЖУ = 99.9 (чуть ниже лимита)', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Чуть ниже лимита');
            await productsPage.fillProteins('49.9');
            await productsPage.fillFats('30.0');
            await productsPage.fillCarbs('20.0');
            await productsPage.selectCategory('Овощи');
            await productsPage.clickSave();

            await expect(productsPage.getProductCardByText('Чуть ниже лимита')).toBeVisible();
        });

        test('Отрицательные значения БЖУ - ошибка', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Отрицательный продукт');
            await productsPage.fillProteins('-10.5');
            await productsPage.fillFats('-5.2');
            await productsPage.fillCarbs('-20.1');
            await productsPage.selectCategory('Овощи');
            
            const responsePromise = productsPage.page.waitForResponse(
                response => response.url().includes('/api/Products')
            );
            
            await productsPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
            
            const errorText = await response.text();
            expect(errorText).toContain('Белки должны быть от 0 до 100');
        });

        test('Калории с плавающей точкой (77.7)', async () => {
            await productsPage.clickNewProductButton();
            await productsPage.fillProductName('Продукт с float калориями');
            await productsPage.fillCalories('77.7');
            await productsPage.fillProteins(10);
            await productsPage.fillFats(5);
            await productsPage.fillCarbs(20);
            await productsPage.selectCategory('Овощи');
            await productsPage.clickSave();

            await expect(productsPage.getProductCardByText('Продукт с float калориями')).toBeVisible();
        });
    });
});