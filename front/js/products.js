let editingId = null;

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
        renderTable(products);
    } catch (e) {
        alert(e.message);
    }
}

function renderTable(products) {
    const tbody = document.querySelector('#productsTable tbody');
    tbody.innerHTML = products.map(p => `
        <tr>
            <td>${p.photos?.length > 0 ? `<img src="${p.photos[0]}" width="50" onerror="this.style.display='none'">` : '—'}</td>
            <td>${p.name}</td>
            <td>${p.calories}</td>
            <td>${p.proteins}</td>
            <td>${p.fats}</td>
            <td>${p.carbs}</td>
            <td>${p.category}</td>
            <td>${p.flags}</td>
            <td>
                <button onclick="showEditForm('${p.id}')">✏️</button>
                <button class="danger" onclick="deleteProduct('${p.id}')">🗑️</button>
            </td>
        </tr>
    `).join('');
}

function showCreateForm() {
    editingId = null;
    document.getElementById('modalTitle').textContent = 'Новый продукт';
    document.getElementById('productForm').reset();
    document.getElementById('productId').value = '';
    document.getElementById('productModal').style.display = 'flex';
}

async function showEditForm(id) {
    try {
        const product = await apiGet(`/Products/${id}`);
        editingId = id;
        document.getElementById('modalTitle').textContent = 'Редактировать продукт';
        document.getElementById('productId').value = product.id;
        document.getElementById('name').value = product.name;
        document.getElementById('photos').value = (product.photos || []).join(', ');
        document.getElementById('calories').value = product.calories;
        document.getElementById('proteins').value = product.proteins;
        document.getElementById('fats').value = product.fats;
        document.getElementById('carbs').value = product.carbs;
        document.getElementById('category').value = product.category;
        document.getElementById('cookingRequirement').value = product.cookingRequirement;
        
        document.getElementById('flagVegan').checked = product.flags.includes('Веган');
        document.getElementById('flagGluten').checked = product.flags.includes('Без_глютена');
        document.getElementById('flagSugar').checked = product.flags.includes('Без_сахара');
        
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

function getPhotosArray() {
    const val = document.getElementById('photos').value;
    return val ? val.split(',').map(s => s.trim()).filter(s => s) : [];
}

document.getElementById('productForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const data = {
        name: document.getElementById('name').value,
        photos: getPhotosArray(),
        calories: parseFloat(document.getElementById('calories').value),
        proteins: parseFloat(document.getElementById('proteins').value),
        fats: parseFloat(document.getElementById('fats').value),
        carbs: parseFloat(document.getElementById('carbs').value),
        composition: null,
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