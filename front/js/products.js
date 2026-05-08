const MAX_PHOTOS = 5;
let editingId = null;
let uploadedPhotos = [];

async function loadProducts() {
    const search = document.getElementById('search').value;
    const category = document.getElementById('filterCategory').value;
    const cooking = document.getElementById('filterCooking').value;
    const flags = document.getElementById('filterFlags').value;
    const sortBy = document.getElementById('sortBy').value;

    let params = [];
    if (category) params.push(`category=${category}`);
    if (cooking) params.push(`cookingRequirement=${cooking}`);
    if (flags) params.push(`flags=${flags}`);
    if (search) params.push(`search=${search}`);
    if (sortBy) params.push(`sortBy=${sortBy}`);

    const query = params.length ? '?' + params.join('&') : '';

    try {
        const products = await apiGet(`/Products${query}`);
        renderCards(products);
    } catch (e) {
        alert(e.message);
    }
}

function renderCards(products) {
    const container = document.getElementById('productsCards');
    container.innerHTML = products.map(p => `
        <div class="card">
            ${p.photos?.length > 0 
                ? `<img class="card-img" src="${p.photos[0].startsWith('/') ? 'http://localhost:5187' + p.photos[0] : p.photos[0]}" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>'">`
                : `<img class="card-img" src="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>">`
            }
            <div class="card-body">
                <div class="card-title">${p.name}</div>
                <div class="card-stats">
                    <div class="stat">🔥 <span>${p.calories}</span> ккал</div>
                    <div class="stat">🟤 <span>${p.proteins}</span> б</div>
                    <div class="stat">🟡 <span>${p.fats}</span> ж</div>
                    <div class="stat">🟠 <span>${p.carbs}</span> у</div>
                </div>
                <div class="card-tags">
                    <span class="tag">${p.category}</span>
                    <span class="tag orange">${p.cookingRequirement.replace(/_/g, ' ')}</span>
                    ${p.flags !== 'None' ? p.flags.split(', ').map(f => `<span class="tag">${f}</span>`).join('') : ''}
                </div>
                ${p.usedInDishes?.length > 0 ? `<div style="font-size:11px;color:#999;margin-bottom:5px;">Используется: ${p.usedInDishes.join(', ')}</div>` : ''}
                <div class="card-actions">
                    <button onclick="showEditForm('${p.id}')">✏️</button>
                    <button class="danger" onclick="deleteProduct('${p.id}')">🗑️</button>
                </div>
            </div>
        </div>
    `).join('');
}

function showCreateForm() {
    editingId = null;
    uploadedPhotos = [];
    document.getElementById('modalTitle').textContent = 'Новый продукт';
    document.getElementById('productForm').reset();
    document.getElementById('productId').value = '';
    document.getElementById('photos').value = '';
    document.getElementById('photoPreviews').innerHTML = '';
    document.getElementById('productModal').style.display = 'flex';
}

async function showEditForm(id) {
    try {
        const product = await apiGet(`/Products/${id}`);
        editingId = id;
        uploadedPhotos = product.photos || [];
        
        document.getElementById('modalTitle').textContent = 'Редактировать продукт';
        document.getElementById('productId').value = product.id;
        document.getElementById('name').value = product.name;
        document.getElementById('photos').value = uploadedPhotos.join(', ');
        document.getElementById('composition').value = product.composition || '';
        document.getElementById('calories').value = product.calories;
        document.getElementById('proteins').value = product.proteins;
        document.getElementById('fats').value = product.fats;
        document.getElementById('carbs').value = product.carbs;
        document.getElementById('category').value = product.category;
        document.getElementById('cookingRequirement').value = product.cookingRequirement;

        document.getElementById('flagVegan').checked = product.flags.includes('Веган');
        document.getElementById('flagGluten').checked = product.flags.includes('Без_глютена');
        document.getElementById('flagSugar').checked = product.flags.includes('Без_сахара');

        renderPhotoPreviews();
        document.getElementById('productModal').style.display = 'flex';
    } catch (e) {
        alert(e.message);
    }
}

function closeModal() {
    document.getElementById('productModal').style.display = 'none';
}

function getFlagsString() {
    const flags = [];
    if (document.getElementById('flagVegan').checked) flags.push('Веган');
    if (document.getElementById('flagGluten').checked) flags.push('Без_глютена');
    if (document.getElementById('flagSugar').checked) flags.push('Без_сахара');
    return flags.join(', ');
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
            const response = await fetch('http://localhost:5187/api/Upload', {
                method: 'POST',
                body: formData
            });
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

document.getElementById('productForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const data = {
        name: document.getElementById('name').value,
        photos: uploadedPhotos,
        calories: parseFloat(document.getElementById('calories').value),
        proteins: parseFloat(document.getElementById('proteins').value),
        fats: parseFloat(document.getElementById('fats').value),
        carbs: parseFloat(document.getElementById('carbs').value),
        composition: document.getElementById('composition').value || null,
        category: document.getElementById('category').value,
        cookingRequirement: document.getElementById('cookingRequirement').value,
        flags: getFlagsString()
    };

    try {
        if (editingId) {
            await apiPut(`/Products/${editingId}`, data);
        } else {
            await apiPost('/Products', data);
        }
        closeModal();
        loadProducts();
    } catch (e) {
        alert(e.message);
    }
});

async function deleteProduct(id) {
    if (!confirm('Удалить продукт?')) return;
    try {
        await apiDelete(`/Products/${id}`);
        loadProducts();
    } catch (e) {
        alert(e.message);
    }
}

loadProducts();