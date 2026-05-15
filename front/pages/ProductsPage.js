// pages/ProductsPage.js
class ProductsPage {
    constructor(page) {
        this.page = page;
    }

    async goto() {
        await this.page.goto('http://127.0.0.1:3000/products.html');
    }

    async clickNewProductButton() {
        await this.page.getByTestId('new-product-btn').click();
    }

    async fillProductName(name) {
        await this.page.getByTestId('product-name').fill(name);
    }

    async fillCalories(calories) {
        await this.page.getByTestId('product-calories').fill(String(calories));
    }

    async fillProteins(proteins) {
        await this.page.getByTestId('product-proteins').fill(String(proteins));
    }

    async fillFats(fats) {
        await this.page.getByTestId('product-fats').fill(String(fats));
    }

    async fillCarbs(carbs) {
        await this.page.getByTestId('product-carbs').fill(String(carbs));
    }

    async selectCategory(category) {
        await this.page.getByTestId('product-category').selectOption(category);
    }

    async clickSave() {
        await this.page.getByTestId('save-product-btn').click();
    }

    getProductCardByText(text) {
        return this.page.getByTestId('product-title').filter({ hasText: text }).first();
    }

    getDeleteButtonByProductName(productName) {
        const card = this.page.getByTestId('product-card').filter({ hasText: productName }).first();
        return card.getByTestId('delete-product-btn');
    }
    
}

module.exports = { ProductsPage };