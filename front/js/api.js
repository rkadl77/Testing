const API_URL = 'http://localhost:5187/api';

async function apiGet(url) {
    const response = await fetch(`${API_URL}${url}`);
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Ошибка запроса');
    }
    return response.json();
}

async function apiPost(url, data) {
    const response = await fetch(`${API_URL}${url}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Ошибка создания');
    }
    return response.json();
}

async function apiPut(url, data) {
    const response = await fetch(`${API_URL}${url}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Ошибка обновления');
    }
    return response.json();
}

async function apiDelete(url) {
    const response = await fetch(`${API_URL}${url}`, { method: 'DELETE' });
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Ошибка удаления');
    }
}