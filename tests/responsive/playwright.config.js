// Pruebas responsive de Tren al Sur.
// Antes de ejecutarlas, la app debe estar corriendo: dotnet run --launch-profile http
const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
    testDir: '.',
    globalSetup: require.resolve('./global-setup'),
    timeout: 60000,
    expect: { timeout: 5000 },
    workers: 2, // pocas peticiones a la vez: la app consulta una base remota
    reporter: 'list',
    outputDir: 'resultados',
    use: {
        baseURL: process.env.BASE_URL || 'http://localhost:5187',
        browserName: 'chromium',
        trace: 'retain-on-failure'
    }
});
