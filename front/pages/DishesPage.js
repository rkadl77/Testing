// pages/DishesPage.js
class DishesPage {
    constructor(page) {
        this.page = page;
    }

    async goto() {
        await this.page.goto('http://127.0.0.1:3000/dishes.html');
    }

    async clickNewDishButton() {
        await this.page.getByTestId('new-dish-btn').click();
    }

    async fillDishName(name) {
        await this.page.getByTestId('dish-name').fill(name);
    }

    async fillPortionSize(portionSize) {
        await this.page.getByTestId('dish-portion-size').fill(String(portionSize));
    }

    async fillCalories(calories) {
        await this.page.getByTestId('dish-calories').fill(String(calories));
    }

    async fillProteins(proteins) {
        await this.page.getByTestId('dish-proteins').fill(String(proteins));
    }

    async fillFats(fats) {
        await this.page.getByTestId('dish-fats').fill(String(fats));
    }

    async fillCarbs(carbs) {
        await this.page.getByTestId('dish-carbs').fill(String(carbs));
    }

    async selectCategory(category) {
        await this.page.getByTestId('dish-category').selectOption(category);
    }

    async clickSave() {
        await this.page.getByTestId('save-dish-btn').click();
    }

    async clickAutoCalculate() {
        await this.page.getByTestId('auto-calculate-btn').click();
    }

    async addIngredient() {
        await this.page.getByTestId('add-ingredient-btn').click();
    }

    async selectIngredientByProductId(index, productId) {
        const rows = this.page.getByTestId('ingredient-row');
        const row = rows.nth(index);
        await row.getByTestId('ingredient-select').selectOption(productId);
    }

    async setIngredientQuantity(index, quantity) {
        const rows = this.page.getByTestId('ingredient-row');
        const row = rows.nth(index);
        await row.getByTestId('ingredient-quantity').fill(String(quantity));
    }

    async removeIngredient(index) {
        const rows = this.page.getByTestId('ingredient-row');
        const row = rows.nth(index);
        await row.getByTestId('ingredient-remove').click();
    }

    getDishCardByText(text) {
        return this.page.getByTestId('dish-title').filter({ hasText: text }).first();
    }

    getDeleteButtonByDishName(dishName) {
        const card = this.page.getByTestId('dish-card').filter({ hasText: dishName }).first();
        return card.getByTestId('delete-dish-btn');
    }

    async getCaloriesValue() {
        return await this.page.getByTestId('dish-calories').inputValue();
    }

    getDialog() {
        return this.page.getByRole('dialog');
    }

    async isModalVisible() {
        return await this.page.getByRole('dialog').isVisible().catch(() => false);
    }
}

module.exports = { DishesPage };