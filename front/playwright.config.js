module.exports = {
    testDir: './tests',
    timeout: 30000,
    use: {
        baseURL: 'http://127.0.0.1:3000',
        headless: false,
        viewport: { width: 1280, height: 720 },
    },
};