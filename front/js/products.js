const MAX_PHOTOS = 5;
let editingId = null;
let uploadedPhotos = [];

// Хранилище текущих индексов фото для каждого продукта
let productPhotoIndex = {};

function getImageUrl(url) {
    if (!url) return '';
    return url.startsWith('/') ? 'http://localhost:5187' + url : url;
}

// ДОБАВИТЬ: функция форматирования даты
function formatDate(dateString) {
    if (!dateString) return '—';
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

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
    container.innerHTML = products.map(p => {
        const photos = p.photos || [];
        if (productPhotoIndex[p.id] === undefined) {
            productPhotoIndex[p.id] = 0;
        }
        
        return `
        <div class="card" data-product-id="${p.id}">
            <div style="position:relative; width:100%; height:180px; background:#f0f0f0; overflow:hidden; cursor:pointer;" onclick="viewProduct('${p.id}')">
                ${photos.length > 0 ? `
                    <img class="card-img gallery-img-${p.id}" src="${getImageUrl(photos[0])}" style="width:100%; height:100%; object-fit:cover;" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>'">
                    ${photos.length > 1 ? `
                        <button class="gallery-nav gallery-prev" style="position:absolute; top:50%; left:10px; transform:translateY(-50%); background:rgba(0,0,0,0.6); color:white; border:none; border-radius:50%; width:32px; height:32px; cursor:pointer; font-size:18px; z-index:10;" onclick="event.stopPropagation(); changeProductPhoto('${p.id}', -1)">‹</button>
                        <button class="gallery-nav gallery-next" style="position:absolute; top:50%; right:10px; transform:translateY(-50%); background:rgba(0,0,0,0.6); color:white; border:none; border-radius:50%; width:32px; height:32px; cursor:pointer; font-size:18px; z-index:10;" onclick="event.stopPropagation(); changeProductPhoto('${p.id}', 1)">›</button>
                        <div style="position:absolute; bottom:10px; left:50%; transform:translateX(-50%); display:flex; gap:6px; z-index:10;">
                            ${photos.map((_, idx) => `<span class="gallery-dot-${p.id}" data-idx="${idx}" style="width:8px; height:8px; border-radius:50%; background:${idx === 0 ? 'white' : 'rgba(255,255,255,0.5)'}; cursor:pointer;" onclick="event.stopPropagation(); setProductPhotoIndex('${p.id}', ${idx})"></span>`).join('')}
                        </div>
                    ` : ''}
                ` : `
                    <img class="card-img" src="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>" style="width:100%; height:100%; object-fit:cover;">
                `}
            </div>
            <div class="card-body">
                <div class="card-title">${p.name}</div>
                <div class="meta" style="display:flex; gap:12px; font-size:0.7rem; color:#666; margin-bottom:8px; padding-bottom:6px; border-bottom:1px solid #eee;">
                    <span>📅 ${formatDate(p.createdAt)}</span>
                    ${p.updatedAt ? `<span>✏️ ${formatDate(p.updatedAt)}</span>` : ''}
                </div>
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
                    <button onclick="event.stopPropagation(); viewProduct('${p.id}')">👁️ Просмотр</button>
                    <button onclick="event.stopPropagation(); showEditForm('${p.id}')">✏️</button>
                    <button class="danger" onclick="event.stopPropagation(); deleteProduct('${p.id}')">🗑️</button>
                </div>
            </div>
        </div>
    `}).join('');
}

// Функция для смены фото в карточке продукта
function changeProductPhoto(productId, direction) {
    // Нужно получить актуальные данные продукта
    fetch(`http://localhost:5187/api/Products/${productId}`)
        .then(res => res.json())
        .then(product => {
            if (!product.photos || product.photos.length === 0) return;
            
            let currentIndex = productPhotoIndex[productId] || 0;
            let newIndex = currentIndex + direction;
            
            if (newIndex < 0) newIndex = product.photos.length - 1;
            if (newIndex >= product.photos.length) newIndex = 0;
            
            productPhotoIndex[productId] = newIndex;
            
            const img = document.querySelector(`.gallery-img-${productId}`);
            if (img) {
                img.src = getImageUrl(product.photos[newIndex]);
            }
            
            for (let i = 0; i < product.photos.length; i++) {
                const dot = document.querySelector(`.gallery-dot-${productId}[data-idx="${i}"]`);
                if (dot) {
                    dot.style.background = i === newIndex ? 'white' : 'rgba(255,255,255,0.5)';
                }
            }
        })
        .catch(e => console.error(e));
}

// Функция для установки конкретного индекса фото
function setProductPhotoIndex(productId, index) {
    fetch(`http://localhost:5187/api/Products/${productId}`)
        .then(res => res.json())
        .then(product => {
            if (!product.photos || index >= product.photos.length) return;
            
            productPhotoIndex[productId] = index;
            
            const img = document.querySelector(`.gallery-img-${productId}`);
            if (img) {
                img.src = getImageUrl(product.photos[index]);
            }
            
            for (let i = 0; i < product.photos.length; i++) {
                const dot = document.querySelector(`.gallery-dot-${productId}[data-idx="${i}"]`);
                if (dot) {
                    dot.style.background = i === index ? 'white' : 'rgba(255,255,255,0.5)';
                }
            }
        })
        .catch(e => console.error(e));
}

// Новая функция для просмотра продукта с галереей
async function viewProduct(id) {
    try {
        const product = await apiGet(`/Products/${id}`);
        const photos = product.photos || [];
        let currentPhotoIndex = 0;
        
        const modal = document.createElement('div');
        modal.className = 'modal';
        modal.style.display = 'flex';
        
        function updateModalImage() {
            const img = modal.querySelector('.modal-gallery-img');
            if (img && photos.length > 0) {
                img.src = getImageUrl(photos[currentPhotoIndex]);
            }
            for (let i = 0; i < photos.length; i++) {
                const dot = modal.querySelector(`.modal-dot-${i}`);
                if (dot) {
                    dot.style.background = i === currentPhotoIndex ? 'white' : 'rgba(255,255,255,0.5)';
                }
            }
        }
        
        modal.innerHTML = `
            <div class="modal-content" style="max-width: 500px;">
                <h2>📦 ${product.name}</h2>
                <div style="position:relative; width:100%; height:250px; background:#f0f0f0; border-radius:8px; overflow:hidden; margin-bottom:15px;">
                    ${photos.length > 0 ? `
                        <img class="modal-gallery-img" src="${getImageUrl(photos[0])}" style="width:100%; height:100%; object-fit:cover;">
                        ${photos.length > 1 ? `
                            <button class="modal-prev" style="position:absolute; top:50%; left:10px; transform:translateY(-50%); background:rgba(0,0,0,0.6); color:white; border:none; border-radius:50%; width:32px; height:32px; cursor:pointer; font-size:18px; z-index:10;">‹</button>
                            <button class="modal-next" style="position:absolute; top:50%; right:10px; transform:translateY(-50%); background:rgba(0,0,0,0.6); color:white; border:none; border-radius:50%; width:32px; height:32px; cursor:pointer; font-size:18px; z-index:10;">›</button>
                            <div style="position:absolute; bottom:10px; left:50%; transform:translateX(-50%); display:flex; gap:6px; z-index:10;">
                                ${photos.map((_, idx) => `<span class="modal-dot-${idx}" data-idx="${idx}" style="width:8px; height:8px; border-radius:50%; background:${idx === 0 ? 'white' : 'rgba(255,255,255,0.5)'}; cursor:pointer;"></span>`).join('')}
                            </div>
                        ` : ''}
                    ` : `
                        <div style="display:flex; align-items:center; justify-content:center; height:100%; color:#999;">Нет фото</div>
                    `}
                </div>
                <div style="display:grid; grid-template-columns:1fr 1fr; gap:10px; margin-bottom:15px;">
                    <div><strong>🔥 Калории:</strong> ${product.calories} ккал</div>
                    <div><strong>🟤 Белки:</strong> ${product.proteins} г</div>
                    <div><strong>🟡 Жиры:</strong> ${product.fats} г</div>
                    <div><strong>🟠 Углеводы:</strong> ${product.carbs} г</div>
                </div>
                <div style="margin-bottom:10px;">
                    <strong>📂 Категория:</strong> ${product.category}<br>
                    <strong>🍳 Готовность:</strong> ${product.cookingRequirement.replace(/_/g, ' ')}<br>
                    <strong>🏷️ Флаги:</strong> ${product.flags !== 'None' ? product.flags : 'Нет'}<br>
                    ${product.composition ? `<strong>📋 Состав:</strong> ${product.composition}<br>` : ''}
                    ${product.usedInDishes?.length > 0 ? `<strong>🍽️ Используется в блюдах:</strong> ${product.usedInDishes.join(', ')}<br>` : ''}
                    <strong>📅 Создан:</strong> ${formatDate(product.createdAt)}<br>
                    ${product.updatedAt ? `<strong>✏️ Изменён:</strong> ${formatDate(product.updatedAt)}` : ''}
                </div>
                <div class="form-actions">
                    <button onclick="this.closest('.modal').remove()">Закрыть</button>
                    <button onclick="this.closest('.modal').remove(); showEditForm('${product.id}')">✏️ Редактировать</button>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        if (photos.length > 1) {
            const prevBtn = modal.querySelector('.modal-prev');
            const nextBtn = modal.querySelector('.modal-next');
            
            if (prevBtn) {
                prevBtn.onclick = () => {
                    currentPhotoIndex = (currentPhotoIndex - 1 + photos.length) % photos.length;
                    updateModalImage();
                };
            }
            if (nextBtn) {
                nextBtn.onclick = () => {
                    currentPhotoIndex = (currentPhotoIndex + 1) % photos.length;
                    updateModalImage();
                };
            }
            
            for (let i = 0; i < photos.length; i++) {
                const dot = modal.querySelector(`.modal-dot-${i}`);
                if (dot) {
                    dot.onclick = () => {
                        currentPhotoIndex = i;
                        updateModalImage();
                    };
                }
            }
        }
        
        modal.addEventListener('click', (e) => {
            if (e.target === modal) modal.remove();
        });
    } catch (e) {
        alert(e.message);
    }
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
    
    return flags.length === 0 ? 'None' : flags.join(', ');
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