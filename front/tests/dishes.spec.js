// tests/dishes.spec.js
const { test, expect } = require('@playwright/test');
const { ProductsPage } = require('../pages/ProductsPage');
const { DishesPage } = require('../pages/DishesPage');

test.describe('Блюда - UI тесты', () => {
    let productsPage;
    let dishesPage;
    let productId;

    test.beforeEach(async ({ page }) => {
        productsPage = new ProductsPage(page);
        dishesPage = new DishesPage(page);
        
        await productsPage.goto();
        await productsPage.clickNewProductButton();
        await productsPage.fillProductName('Картофель для теста');
        await productsPage.fillCalories(77);
        await productsPage.fillProteins(2);
        await productsPage.fillFats(0.4);
        await productsPage.fillCarbs(16.3);
        await productsPage.selectCategory('Овощи');
        
        const responsePromise = productsPage.page.waitForResponse(
            response => response.url().includes('/api/Products') && response.status() === 201
        );
        
        await productsPage.clickSave();
        
        const response = await responsePromise;
        const product = await response.json();
        productId = product.id;
        
        await dishesPage.goto();
    });

    test.describe('Эквивалентное разбиение', () => {
        
        test('Создание блюда с корректными данными', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('Картофельный суп');
            await dishesPage.fillPortionSize(500);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 200);
            await dishesPage.selectCategory('Суп');
            await dishesPage.clickAutoCalculate();

            const calories = await dishesPage.getCaloriesValue();
            expect(parseFloat(calories)).toBeGreaterThan(0);
            
            await dishesPage.clickSave();
            await expect(dishesPage.getDishCardByText('Картофельный суп')).toBeVisible();
        });

        test('Создание блюда с пустым названием - ошибка', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('');
            await dishesPage.fillPortionSize(500);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 200);
            await dishesPage.selectCategory('Суп');
            
            const responsePromise = dishesPage.page.waitForResponse(
                response => response.url().includes('/api/Dishes')
            );
            
            await dishesPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
        });

        test('Создание блюда без ингредиентов - ошибка', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('Пустое блюдо');
            await dishesPage.fillPortionSize(500);
            await dishesPage.selectCategory('Суп');
            
            await dishesPage.clickSave();
            
            await expect(dishesPage.getDishCardByText('Пустое блюдо')).toBeHidden();
        });
    });

    test.describe('Анализ граничных значений', () => {
        
        test('Название из 2 символов (мин. длина)', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('АБ');
            await dishesPage.fillPortionSize(500);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 200);
            await dishesPage.selectCategory('Суп');
            await dishesPage.clickAutoCalculate();
            await dishesPage.clickSave();

            await expect(dishesPage.getDishCardByText('АБ')).toBeVisible();
        });

        test('Название из 1 символа (ниже мин. длины) - ошибка', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('А');
            await dishesPage.fillPortionSize(500);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 200);
            await dishesPage.selectCategory('Суп');
            
            const responsePromise = dishesPage.page.waitForResponse(
                response => response.url().includes('/api/Dishes')
            );
            
            await dishesPage.clickSave();
            
            const response = await responsePromise;
            expect(response.status()).toBe(400);
        });

        test('Размер порции = 0.1г (мин. значение)', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('Микро порция');
            await dishesPage.fillPortionSize(0.1);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 0.1);
            await dishesPage.selectCategory('Суп');
            await dishesPage.clickAutoCalculate();
            await dishesPage.clickSave();

            await expect(dishesPage.getDishCardByText('Микро порция')).toBeVisible();
        });
    });

    test.describe('Корнер кейсы - Удаление', () => {
        
        test('Удаление блюда - блюдо исчезает из списка', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('Блюдо для удаления');
            await dishesPage.fillPortionSize(500);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 200);
            await dishesPage.selectCategory('Суп');
            await dishesPage.clickAutoCalculate();
            await dishesPage.clickSave();
            
            await expect(dishesPage.getDishCardByText('Блюдо для удаления')).toBeVisible();
            
            const deleteBtn = dishesPage.getDeleteButtonByDishName('Блюдо для удаления');
            
            dishesPage.page.once('dialog', async dialog => {
                expect(dialog.message()).toContain('Удалить блюдо?');
                await dialog.accept();
            });
            
            await deleteBtn.click();
            
            await expect(dishesPage.getDishCardByText('Блюдо для удаления')).toBeHidden();
        });

        test('Попытка удалить продукт, который используется в блюде - продукт не удаляется', async () => {
            await dishesPage.clickNewDishButton();
            await dishesPage.fillDishName('Суп с картофелем');
            await dishesPage.fillPortionSize(500);
            await dishesPage.addIngredient();
            await dishesPage.selectIngredientByProductId(0, productId);
            await dishesPage.setIngredientQuantity(0, 200);
            await dishesPage.selectCategory('Суп');
            await dishesPage.clickAutoCalculate();
            await dishesPage.clickSave();
            
            await expect(dishesPage.getDishCardByText('Суп с картофелем')).toBeVisible();
            
            await productsPage.goto();
            
            await expect(productsPage.getProductCardByText('Картофель для теста')).toBeVisible();
            
            const deleteBtn = productsPage.getDeleteButtonByProductName('Картофель для теста');
            
            productsPage.page.once('dialog', async dialog => {
                await dialog.accept();
            });
            
            await deleteBtn.click();
            
            await expect(productsPage.getProductCardByText('Картофель для теста')).toBeVisible();
        });
    });
});