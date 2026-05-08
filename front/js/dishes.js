const MAX_PHOTOS = 5;
let editingId = null;
let allProducts = [];
let uploadedPhotos = [];

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
    div.style.cssText = 'display:flex; gap:10px; margin-bottom:8px;';
    div.innerHTML = `
        <select class="product-select" style="flex:2;">
            <option value="">Выберите продукт...</option>
        </select>
        <input type="number" class="quantity-input" placeholder="г" step="0.1" min="0.1" style="width:80px;">
        <button type="button" class="danger" onclick="removeIngredientRow(this)" style="flex:0;">✕</button>
    `;
    document.getElementById('ingredients').appendChild(div);
    populateProductSelects();
    div.querySelector('.product-select').addEventListener('change', autoCalculate);
    div.querySelector('.quantity-input').addEventListener('input', autoCalculate);
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
        renderCards(dishes);
    } catch (e) {
        alert(e.message);
    }
}

function renderCards(dishes) {
    const container = document.getElementById('dishesCards');
    container.innerHTML = dishes.map(d => `
        <div class="card">
            ${d.photos?.length > 0 
                ? `<img class="card-img" src="${d.photos[0].startsWith('/') ? 'http://localhost:5187' + d.photos[0] : d.photos[0]}" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>'">`
                : `<img class="card-img" src="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>">`
            }
            <div class="card-body">
                <div class="card-title">${d.name}</div>
                <div class="card-stats">
                    <div class="stat">🔥 <span>${d.calories}</span> ккал</div>
                    <div class="stat">🟤 <span>${d.proteins}</span> б</div>
                    <div class="stat">🟡 <span>${d.fats}</span> ж</div>
                    <div class="stat">🟠 <span>${d.carbs}</span> у</div>
                </div>
                <div style="font-size:13px;color:#777;margin-bottom:6px;">Порция: <b>${d.portionSize} г</b></div>
                <div class="card-tags">
                    <span class="tag">${d.category}</span>
                    ${d.flags !== 'None' ? d.flags.split(', ').map(f => `<span class="tag">${f}</span>`).join('') : ''}
                </div>
                ${d.ingredients?.length > 0 ? `<div style="font-size:11px;color:#999;">Состав: ${d.ingredients.map(i => `${i.productName} (${i.quantity}г)`).join(', ')}</div>` : ''}
                <div class="card-actions">
                    <button onclick="showEditForm('${d.id}')">✏️</button>
                    <button class="danger" onclick="deleteDish('${d.id}')">🗑️</button>
                </div>
            </div>
        </div>
    `).join('');
}

async function showCreateForm() {
    editingId = null;
    uploadedPhotos = [];
    document.getElementById('modalTitle').textContent = 'Новое блюдо';
    document.getElementById('dishForm').reset();
    document.getElementById('dishId').value = '';
    document.getElementById('photos').value = '';
    document.getElementById('photoPreviews').innerHTML = '';
    document.getElementById('ingredients').innerHTML = '';
    addIngredientRow();
    populateProductSelects();
    document.getElementById('dishModal').style.display = 'flex';
}

async function showEditForm(id) {
    try {
        const dish = await apiGet(`/Dishes/${id}`);
        editingId = id;
        uploadedPhotos = dish.photos || [];

        document.getElementById('modalTitle').textContent = 'Редактировать блюдо';
        document.getElementById('dishId').value = dish.id;
        document.getElementById('name').value = dish.name;
        document.getElementById('photos').value = uploadedPhotos.join(', ');
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
        renderPhotoPreviews();
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
    return flags.join(', ');
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

async function uploadPhotos() {
    const files = document.getElementById('photoInput').files;
    if (!files.length) return;
    if (uploadedPhotos.length + files.length > MAX_PHOTOS) {
        alert(`Можно добавить не более ${MAX_PHOTOS} фото. Сейчас уже ${uploadedPhotos.length}.`);
        return;
    }
    for (const file of files) {
        if (uploadedPhotos.length >= MAX_PHOTOS) break;
        const formData = new FormData();
        formData.append('file', file);
        try {
            const response = await fetch('http://localhost:5187/api/Upload', { method: 'POST', body: formData });
            if (!response.ok) throw new Error('Ошибка загрузки');
            const data = await response.json();
            uploadedPhotos.push(data.url);
        } catch (e) {
            alert('Не удалось загрузить фото: ' + file.name);
        }
    }
    document.getElementById('photos').value = uploadedPhotos.join(', ');
    renderPhotoPreviews();
    document.getElementById('photoInput').value = '';
}

function addPhotoUrl() {
    const url = document.getElementById('photoUrl').value.trim();
    if (!url) return;
    if (uploadedPhotos.length >= MAX_PHOTOS) {
        alert(`Можно добавить не более ${MAX_PHOTOS} фото.`);
        return;
    }
    uploadedPhotos.push(url);
    document.getElementById('photos').value = uploadedPhotos.join(', ');
    document.getElementById('photoUrl').value = '';
    renderPhotoPreviews();
}

function renderPhotoPreviews() {
    const container = document.getElementById('photoPreviews');
    container.innerHTML = uploadedPhotos.map((url, index) => `
        <div class="photo-preview">
            <img src="${url.startsWith('/') ? 'http://localhost:5187' + url : url}" onerror="this.style.display='none'">
            <button type="button" class="remove-btn" onclick="removePhoto(${index})">✕</button>
        </div>
    `).join('');
}

function removePhoto(index) {
    uploadedPhotos.splice(index, 1);
    document.getElementById('photos').value = uploadedPhotos.join(', ');
    renderPhotoPreviews();
}

document.getElementById('portionSize').addEventListener('input', autoCalculate);

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
        photos: uploadedPhotos,
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