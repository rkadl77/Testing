const MAX_PHOTOS = 5;
let editingId = null;
let uploadedPhotos = [];
let allProducts = [];

// Хранилище текущих индексов фото для каждого блюда
let dishPhotoIndex = {};

function getImageUrl(url) {
    if (!url) return '';
    return url.startsWith('/') ? 'http://localhost:5187' + url : url;
}

// Функция форматирования даты
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

async function loadDishes() {
    const search = document.getElementById('search').value;
    const category = document.getElementById('filterCategory').value;
    const flags = document.getElementById('filterFlags').value;

    let params = [];
    if (category) params.push(`category=${category}`);
    if (flags && flags !== 'None') params.push(`flags=${flags}`);
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
    container.innerHTML = dishes.map(d => {
        const photos = d.photos || [];
        if (dishPhotoIndex[d.id] === undefined) {
            dishPhotoIndex[d.id] = 0;
        }
        
        return `
        <div class="card" data-testid="dish-card" data-dish-id="${d.id}">
            <div data-testid="dish-image-area" style="position:relative; width:100%; height:180px; background:#f0f0f0; overflow:hidden; cursor:pointer;" onclick="viewDish('${d.id}')">
                ${photos.length > 0 ? `
                    <img data-testid="dish-image" class="card-img gallery-img-${d.id}" src="${getImageUrl(photos[0])}" style="width:100%; height:100%; object-fit:cover;" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>'">
                    ${photos.length > 1 ? `
                        <button data-testid="gallery-prev" class="gallery-nav gallery-prev" style="position:absolute; top:50%; left:10px; transform:translateY(-50%); background:rgba(0,0,0,0.6); color:white; border:none; border-radius:50%; width:32px; height:32px; cursor:pointer; font-size:18px; z-index:10;" onclick="event.stopPropagation(); changeDishPhoto('${d.id}', -1)">‹</button>
                        <button data-testid="gallery-next" class="gallery-nav gallery-next" style="position:absolute; top:50%; right:10px; transform:translateY(-50%); background:rgba(0,0,0,0.6); color:white; border:none; border-radius:50%; width:32px; height:32px; cursor:pointer; font-size:18px; z-index:10;" onclick="event.stopPropagation(); changeDishPhoto('${d.id}', 1)">›</button>
                        <div data-testid="gallery-dots" style="position:absolute; bottom:10px; left:50%; transform:translateX(-50%); display:flex; gap:6px; z-index:10;">
                            ${photos.map((_, idx) => `<span data-testid="gallery-dot" class="gallery-dot-${d.id}" data-idx="${idx}" style="width:8px; height:8px; border-radius:50%; background:${idx === 0 ? 'white' : 'rgba(255,255,255,0.5)'}; cursor:pointer;" onclick="event.stopPropagation(); setDishPhotoIndex('${d.id}', ${idx})"></span>`).join('')}
                        </div>
                    ` : ''}
                ` : `
                    <img data-testid="no-image" class="card-img" src="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22320%22 height=%22180%22><rect fill=%22%23e0e0e0%22 width=%22320%22 height=%22180%22/><text x=%2230%25%22 y=%2250%25%22 fill=%22%23999%22 font-size=%2218%22>Нет фото</text></svg>" style="width:100%; height:100%; object-fit:cover;">
                `}
            </div>
            <div class="card-body">
                <div data-testid="dish-title" class="card-title">${d.name}</div>
                <div class="meta" style="display:flex; gap:12px; font-size:0.7rem; color:#666; margin-bottom:8px; padding-bottom:6px; border-bottom:1px solid #eee;">
                    <span>📅 ${formatDate(d.createdAt)}</span>
                    ${d.updatedAt ? `<span>✏️ ${formatDate(d.updatedAt)}</span>` : ''}
                </div>
                <div class="card-stats">
                    <div class="stat">🔥 <span data-testid="dish-calories-value">${d.calories}</span> ккал</div>
                    <div class="stat">🟤 <span data-testid="dish-proteins-value">${d.proteins}</span> б</div>
                    <div class="stat">🟡 <span data-testid="dish-fats-value">${d.fats}</span> ж</div>
                    <div class="stat">🟠 <span data-testid="dish-carbs-value">${d.carbs}</span> у</div>
                </div>
                <div class="card-tags">
                    <span data-testid="dish-category-value" class="tag">${d.category}</span>
                    ${d.flags !== 'None' ? d.flags.replace(/_/g, ' ').split(', ').map(f => `<span data-testid="dish-flag" class="tag">${f}</span>`).join('') : '<span class="tag gray">Нет флагов</span>'}
                </div>
                <div class="card-actions">
                    <button data-testid="view-dish-btn" onclick="event.stopPropagation(); viewDish('${d.id}')">👁️ Просмотр</button>
                    <button data-testid="edit-dish-btn" onclick="event.stopPropagation(); showEditForm('${d.id}')">✏️</button>
                    <button data-testid="delete-dish-btn" class="danger" onclick="event.stopPropagation(); deleteDish('${d.id}')">🗑️</button>
                </div>
            </div>
        </div>
    `}).join('');
}

// Функция для смены фото в карточке блюда
function changeDishPhoto(dishId, direction) {
    fetch(`http://localhost:5187/api/Dishes/${dishId}`)
        .then(res => res.json())
        .then(dish => {
            if (!dish.photos || dish.photos.length === 0) return;
            
            let currentIndex = dishPhotoIndex[dishId] || 0;
            let newIndex = currentIndex + direction;
            
            if (newIndex < 0) newIndex = dish.photos.length - 1;
            if (newIndex >= dish.photos.length) newIndex = 0;
            
            dishPhotoIndex[dishId] = newIndex;
            
            const img = document.querySelector(`.gallery-img-${dishId}`);
            if (img) {
                img.src = getImageUrl(dish.photos[newIndex]);
            }
            
            for (let i = 0; i < dish.photos.length; i++) {
                const dot = document.querySelector(`.gallery-dot-${dishId}[data-idx="${i}"]`);
                if (dot) {
                    dot.style.background = i === newIndex ? 'white' : 'rgba(255,255,255,0.5)';
                }
            }
        })
        .catch(e => console.error(e));
}

// Функция для установки конкретного индекса фото
function setDishPhotoIndex(dishId, index) {
    fetch(`http://localhost:5187/api/Dishes/${dishId}`)
        .then(res => res.json())
        .then(dish => {
            if (!dish.photos || index >= dish.photos.length) return;
            
            dishPhotoIndex[dishId] = index;
            
            const img = document.querySelector(`.gallery-img-${dishId}`);
            if (img) {
                img.src = getImageUrl(dish.photos[index]);
            }
            
            for (let i = 0; i < dish.photos.length; i++) {
                const dot = document.querySelector(`.gallery-dot-${dishId}[data-idx="${i}"]`);
                if (dot) {
                    dot.style.background = i === index ? 'white' : 'rgba(255,255,255,0.5)';
                }
            }
        })
        .catch(e => console.error(e));
}

// Функция для просмотра блюда с галереей
async function viewDish(id) {
    try {
        const dish = await apiGet(`/Dishes/${id}`);
        const photos = dish.photos || [];
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
            <div class="modal-content" style="max-width: 600px;">
                <h2>🍽️ ${dish.name}</h2>
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
                    <div><strong>🔥 Калории:</strong> ${dish.calories} ккал</div>
                    <div><strong>🟤 Белки:</strong> ${dish.proteins} г</div>
                    <div><strong>🟡 Жиры:</strong> ${dish.fats} г</div>
                    <div><strong>🟠 Углеводы:</strong> ${dish.carbs} г</div>
                </div>
                <div style="margin-bottom:10px;">
                    <strong>📏 Размер порции:</strong> ${dish.portionSize} г<br>
                    <strong>📂 Категория:</strong> ${dish.category}<br>
                    <strong>🏷️ Флаги:</strong> ${dish.flags !== 'None' ? dish.flags : 'Нет'}<br>
                    <strong>📅 Создан:</strong> ${formatDate(dish.createdAt)}<br>
                    ${dish.updatedAt ? `<strong>✏️ Изменён:</strong> ${formatDate(dish.updatedAt)}` : ''}
                </div>
                <h3>📋 Состав:</h3>
                <ul style="margin-bottom:15px;">
                    ${dish.ingredients.map(ing => `
                        <li>${ing.productName} — ${ing.quantity} г</li>
                    `).join('')}
                </ul>
                <div class="form-actions">
                    <button onclick="this.closest('.modal').remove()">Закрыть</button>
                    <button onclick="this.closest('.modal').remove(); showEditForm('${dish.id}')">✏️ Редактировать</button>
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

async function loadProductsList() {
    try {
        allProducts = await apiGet('/Products');
    } catch (e) {
        console.error(e);
    }
}

// НОВАЯ ФУНКЦИЯ: автоподстановка категории из макроса
function setCategoryFromMacro() {
    const nameInput = document.getElementById('name');
    const categorySelect = document.getElementById('category');
    if (!nameInput || !categorySelect) return;
    
    // Если категория уже выбрана вручную пользователем — не трогаем
    if (categorySelect.value) return;
    
    const macroMap = {
        '!десерт': 'Десерт',
        '!первое': 'Первое',
        '!второе': 'Второе',
        '!напиток': 'Напиток',
        '!салат': 'Салат',
        '!суп': 'Суп',
        '!перекус': 'Перекус'
    };
    
    const name = nameInput.value.toLowerCase();
    for (const [macro, category] of Object.entries(macroMap)) {
        if (name.startsWith(macro.toLowerCase())) {
            categorySelect.value = category;
            break;
        }
    }
}

function showCreateForm() {
    editingId = null;
    uploadedPhotos = [];
    document.getElementById('modalTitle').textContent = 'Новое блюдо';
    document.getElementById('dishForm').reset();
    document.getElementById('dishId').value = '';
    document.getElementById('photos').value = '';
    document.getElementById('photoPreviews').innerHTML = '';
    document.getElementById('ingredients').innerHTML = '';
    document.getElementById('category').value = '';
    addIngredientRow();
    
    // Сброс чекбоксов флагов
    document.getElementById('flagVegan').checked = false;
    document.getElementById('flagGluten').checked = false;
    document.getElementById('flagSugar').checked = false;
    
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
        document.getElementById('calories').value = dish.calories;
        document.getElementById('proteins').value = dish.proteins;
        document.getElementById('fats').value = dish.fats;
        document.getElementById('carbs').value = dish.carbs;
        document.getElementById('category').value = dish.category;

        document.getElementById('flagVegan').checked = dish.flags.includes('Веган');
        document.getElementById('flagGluten').checked = dish.flags.includes('Без_глютена');
        document.getElementById('flagSugar').checked = dish.flags.includes('Без_сахара');

        document.getElementById('ingredients').innerHTML = '';
        for (const ing of dish.ingredients) {
            addIngredientRow(ing.productId, ing.quantity);
        }
        if (dish.ingredients.length === 0) {
            addIngredientRow();
        }

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

async function addIngredientRow(productId = null, quantity = '') {
    if (allProducts.length === 0) {
        await loadProductsList();
    }
    
    const container = document.getElementById('ingredients');
    const row = document.createElement('div');
    row.className = 'ingredient-row';
    row.setAttribute('data-testid', 'ingredient-row');
    row.style.cssText = 'display:flex; gap:10px; margin-bottom:8px; align-items:center;';
    
    const select = document.createElement('select');
    select.className = 'product-select';
    select.setAttribute('data-testid', 'ingredient-select');
    select.style.flex = '2';
    select.innerHTML = '<option value="">Выберите продукт...</option>';
    
    for (const product of allProducts) {
        const option = document.createElement('option');
        option.value = product.id;
        option.textContent = `${product.name} (${product.calories} ккал, Б:${product.proteins} Ж:${product.fats} У:${product.carbs})`;
        if (productId === product.id) {
            option.selected = true;
        }
        select.appendChild(option);
    }
    
    const quantityInput = document.createElement('input');
    quantityInput.type = 'number';
    quantityInput.className = 'quantity-input';
    quantityInput.setAttribute('data-testid', 'ingredient-quantity');
    quantityInput.placeholder = 'г';
    quantityInput.step = '0.1';
    quantityInput.min = '0.1';
    quantityInput.style.width = '80px';
    quantityInput.value = quantity;
    
    const removeBtn = document.createElement('button');
    removeBtn.type = 'button';
    removeBtn.className = 'danger';
    removeBtn.setAttribute('data-testid', 'ingredient-remove');
    removeBtn.style.flex = '0';
    removeBtn.textContent = '✕';
    removeBtn.onclick = () => row.remove();
    
    row.appendChild(select);
    row.appendChild(quantityInput);
    row.appendChild(removeBtn);
    container.appendChild(row);
    
    select.addEventListener('change', autoCalculate);
    quantityInput.addEventListener('input', autoCalculate);
}

function getIngredients() {
    const ingredients = [];
    const rows = document.querySelectorAll('.ingredient-row');
    for (const row of rows) {
        const productId = row.querySelector('.product-select').value;
        const quantity = parseFloat(row.querySelector('.quantity-input').value);
        if (productId && quantity > 0) {
            ingredients.push({ productId, quantity });
        }
    }
    return ingredients;
}

async function autoCalculate() {
    const ingredients = getIngredients();
    if (ingredients.length === 0) return;
    
    try {
        const products = allProducts.length ? allProducts : await apiGet('/Products');
        allProducts = products;
        
        let totalCalories = 0;
        let totalProteins = 0;
        let totalFats = 0;
        let totalCarbs = 0;
        let totalWeight = 0;
        
        for (const ing of ingredients) {
            const product = products.find(p => p.id === ing.productId);
            if (product) {
                const factor = ing.quantity / 100;
                totalCalories += product.calories * factor;
                totalProteins += product.proteins * factor;
                totalFats += product.fats * factor;
                totalCarbs += product.carbs * factor;
                totalWeight += ing.quantity;
            }
        }
        
        // КБЖУ на ВСЁ блюдо (размер порции НЕ ИСПОЛЬЗУЕМ)
        document.getElementById('calories').value = Math.round(totalCalories * 100) / 100;
        document.getElementById('proteins').value = Math.round(totalProteins * 100) / 100;
        document.getElementById('fats').value = Math.round(totalFats * 100) / 100;
        document.getElementById('carbs').value = Math.round(totalCarbs * 100) / 100;
        
        // Опционально: показываем общий вес в поле порции (пользователь может изменить)
        const portionInput = document.getElementById('portionSize');
        if (portionInput && !portionInput.value) {
            portionInput.value = Math.round(totalWeight * 100) / 100;
        }
        
    } catch (e) {
        console.error(e);
    }
}

document.getElementById('dishForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const ingredients = getIngredients();
    if (ingredients.length === 0) {
        alert('Добавьте хотя бы один продукт в состав блюда');
        return;
    }

    const data = {
        name: document.getElementById('name').value,
        photos: uploadedPhotos,
        portionSize: parseFloat(document.getElementById('portionSize').value),
        calories: parseFloat(document.getElementById('calories').value) || 0,
        proteins: parseFloat(document.getElementById('proteins').value) || 0,
        fats: parseFloat(document.getElementById('fats').value) || 0,
        carbs: parseFloat(document.getElementById('carbs').value) || 0,
        category: document.getElementById('category').value,
        flags: getFlagsString(),
        ingredients: ingredients
    };

    // Если категория не выбрана, но есть макрос в названии — отправим пустую строку, бэк сам разберётся
    if (!data.category) {
        // Проверяем, есть ли макрос в названии
        const hasMacro = /^![а-я]+/i.test(data.name);
        if (!hasMacro) {
            alert('Выберите категорию блюда или используйте макрос (!суп, !салат и т.д.)');
            return;
        }
        // Если есть макрос, отправляем пустую строку — бэк определит категорию сам
        data.category = '';
    }

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

// Добавляем обработчик для автоподстановки категории из макроса
if (document.getElementById('name')) {
    document.getElementById('name').addEventListener('input', setCategoryFromMacro);
}

// Инициализация
loadProductsList();
loadDishes();