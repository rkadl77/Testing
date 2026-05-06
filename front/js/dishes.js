let editingId = null;
let allProducts = [];

async function loadAllProducts() {
    try {
        allProducts = await apiGet('/Products');
    } catch (e) {
        console.error('Не удалось загрузить продукты');
    }
}

function populateProductSelects() {
    const selects = document.querySelectorAll('.product-select');
    selects.forEach(select => {
        const currentValue = select.value;
        select.innerHTML = '<option value="">Выберите продукт...</option>';
        allProducts.forEach(p => {
            select.innerHTML += `<option value="${p.id}">${p.name} (${p.calories} ккал, Б:${p.proteins} Ж:${p.fats} У:${p.carbs})</option>`;
        });
        select.value = currentValue;
    });
}

function addIngredientRow() {
    const div = document.createElement('div');
    div.className = 'ingredient-row';
    div.style.cssText = 'display:flex; gap:10px; margin-bottom:5px;';
    div.innerHTML = `
        <select class="product-select" style="flex:1;">
            <option value="">Выберите продукт...</option>
        </select>
        <input type="number" class="quantity-input" placeholder="Граммы" step="0.1" min="0.1" style="width:100px;">
        <button type="button" class="danger" onclick="removeIngredientRow(this)">✕</button>
    `;
    document.getElementById('ingredients').appendChild(div);
    populateProductSelects();

    // Авторасчёт при изменении ингредиента
    div.querySelector('.product-select').addEventListener('change', autoCalculate);
    div.querySelector('.quantity-input').addEventListener('input', autoCalculate);
    div.querySelector('.quantity-input').addEventListener('change', autoCalculate);
}

function removeIngredientRow(btn) {
    btn.parentElement.remove();
    autoCalculate();
}

function autoCalculate() {
    let totalCal = 0, totalProt = 0, totalFat = 0, totalCarbs = 0;
    const rows = document.querySelectorAll('.ingredient-row');

    rows.forEach(row => {
        const productId = row.querySelector('.product-select')?.value;
        const quantity = parseFloat(row.querySelector('.quantity-input')?.value) || 0;

        if (productId && quantity > 0) {
            const product = allProducts.find(p => p.id === productId);
            if (product) {
                totalCal += product.calories * quantity / 100;
                totalProt += product.proteins * quantity / 100;
                totalFat += product.fats * quantity / 100;
                totalCarbs += product.carbs * quantity / 100;
            }
        }
    });

    document.getElementById('calories').value = totalCal.toFixed(2);
    document.getElementById('proteins').value = totalProt.toFixed(2);
    document.getElementById('fats').value = totalFat.toFixed(2);
    document.getElementById('carbs').value = totalCarbs.toFixed(2);
}

async function loadDishes() {
    const search = document.getElementById('search').value;
    const category = document.getElementById('filterCategory').value;
    const flags = document.getElementById('filterFlags').value;

    let params = [];
    if (category) params.push(`category=${category}`);
    if (flags) params.push(`flags=${flags}`);
    if (search) params.push(`search=${search}`);

    const query = params.length ? '?' + params.join('&') : '';

    try {
        const dishes = await apiGet(`/Dishes${query}`);
        renderTable(dishes);
    } catch (e) {
        alert(e.message);
    }
}

function renderTable(dishes) {
    const tbody = document.querySelector('#dishesTable tbody');
    tbody.innerHTML = dishes.map(d => `
        <tr>
            <td>${d.photos?.length > 0 ? `<img src="${d.photos[0]}" width="50" onerror="this.style.display='none'">` : '—'}</td>
            <td>${d.name}</td>
            <td>${d.calories}</td>
            <td>${d.proteins}</td>
            <td>${d.fats}</td>
            <td>${d.carbs}</td>
            <td>${d.portionSize}</td>
            <td>${d.category}</td>
            <td>${d.flags}</td>
            <td>
                <button onclick="showEditForm('${d.id}')">✏️</button>
                <button class="danger" onclick="deleteDish('${d.id}')">🗑️</button>
            </td>
        </tr>
    `).join('');
}

async function showCreateForm() {
    editingId = null;
    document.getElementById('modalTitle').textContent = 'Новое блюдо';
    document.getElementById('dishForm').reset();
    document.getElementById('dishId').value = '';
    document.getElementById('photos').value = '';
    document.getElementById('calories').value = '';
    document.getElementById('proteins').value = '';
    document.getElementById('fats').value = '';
    document.getElementById('carbs').value = '';
    document.getElementById('ingredients').innerHTML = '';
    addIngredientRow();
    populateProductSelects();
    document.getElementById('dishModal').style.display = 'flex';
}

async function showEditForm(id) {
    try {
        const dish = await apiGet(`/Dishes/${id}`);
        editingId = id;

        document.getElementById('modalTitle').textContent = 'Редактировать блюдо';
        document.getElementById('dishId').value = dish.id;
        document.getElementById('name').value = dish.name;
        document.getElementById('photos').value = (dish.photos || []).join(', ');
        document.getElementById('portionSize').value = dish.portionSize;
        document.getElementById('category').value = dish.category;
        document.getElementById('calories').value = dish.calories;
        document.getElementById('proteins').value = dish.proteins;
        document.getElementById('fats').value = dish.fats;
        document.getElementById('carbs').value = dish.carbs;

        document.getElementById('flagVegan').checked = dish.flags.includes('Веган');
        document.getElementById('flagGluten').checked = dish.flags.includes('Без_глютена');
        document.getElementById('flagSugar').checked = dish.flags.includes('Без_сахара');

        document.getElementById('ingredients').innerHTML = '';
        dish.ingredients.forEach(ing => {
            addIngredientRow();
            const rows = document.querySelectorAll('.ingredient-row');
            const lastRow = rows[rows.length - 1];
            lastRow.querySelector('.product-select').value = ing.productId;
            lastRow.querySelector('.quantity-input').value = ing.quantity;
        });
        if (dish.ingredients.length === 0) addIngredientRow();

        populateProductSelects();
        document.getElementById('dishModal').style.display = 'flex';
    } catch (e) {
        alert(e.message);
    }
}

function closeModal() {
    document.getElementById('dishModal').style.display = 'none';
}

function getFlagsString() {
    const flags = [];
    if (document.getElementById('flagVegan').checked) flags.push('Веган');
    if (document.getElementById('flagGluten').checked) flags.push('Без_глютена');
    if (document.getElementById('flagSugar').checked) flags.push('Без_сахара');
    return flags.join(',');
}

function getPhotosArray() {
    const val = document.getElementById('photos').value;
    return val ? val.split(',').map(s => s.trim()).filter(s => s) : [];
}

function getIngredients() {
    const ingredients = [];
    const rows = document.querySelectorAll('.ingredient-row');
    rows.forEach(row => {
        const productId = row.querySelector('.product-select')?.value;
        const quantity = parseFloat(row.querySelector('.quantity-input')?.value);
        if (productId && quantity > 0) {
            ingredients.push({ productId, quantity });
        }
    });
    return ingredients;
}

// Автопересчёт КБЖУ при изменении размера порции
document.getElementById('portionSize').addEventListener('input', autoCalculate);
document.getElementById('portionSize').addEventListener('change', autoCalculate);

document.getElementById('dishForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const ingredients = getIngredients();
    if (ingredients.length === 0) {
        alert('Добавьте хотя бы один продукт в состав блюда');
        return;
    }

    const categoryValue = document.getElementById('category').value;

    const data = {
        name: document.getElementById('name').value,
        photos: getPhotosArray(),
        portionSize: parseFloat(document.getElementById('portionSize').value),
        calories: parseFloat(document.getElementById('calories').value) || null,
        proteins: parseFloat(document.getElementById('proteins').value) || null,
        fats: parseFloat(document.getElementById('fats').value) || null,
        carbs: parseFloat(document.getElementById('carbs').value) || null,
        category: categoryValue || null,
        flags: getFlagsString(),
        ingredients: ingredients
    };

    try {
        if (editingId) {
            await apiPut(`/Dishes/${editingId}`, data);
        } else {
            await apiPost('/Dishes', data);
        }
        closeModal();
        loadDishes();
    } catch (e) {
        alert(e.message);
    }
});

async function deleteDish(id) {
    if (!confirm('Удалить блюдо?')) return;
    try {
        await apiDelete(`/Dishes/${id}`);
        loadDishes();
    } catch (e) {
        alert(e.message);
    }
}

loadAllProducts();
loadDishes();