// Cada página, en cada tamaño de pantalla: sin scroll horizontal, sin errores
// de JavaScript y con una captura para revisarla a ojo.
const { test, expect } = require('@playwright/test');
const { SESION, haySesion, PANTALLAS, abrir, recorrerPagina, medirDesborde } = require('./helpers');

const PUBLICAS = [
    { nombre: 'inicio', url: '/' },
    { nombre: 'rutas', url: '/Services' },
    { nombre: 'ruta-detalle', desde: '/Services', enlace: 'a[href*="/Services/Details/"]' },
    { nombre: 'productos', url: '/Products' },
    { nombre: 'producto-detalle', desde: '/Products', enlace: 'a[href*="/Products/Details/"]' },
    { nombre: 'login', url: '/Account/Login' },
    { nombre: 'registro', url: '/Account/Register' },
    { nombre: 'olvide-contrasena', url: '/Account/ForgotPassword' }
];

const CLIENTE = [
    { nombre: 'carrito', url: '/Cart' },
    { nombre: 'favoritos', url: '/Favorites' },
    { nombre: 'mis-pedidos', url: '/Orders' },
    { nombre: 'mis-reservas', url: '/Reservations' }
];

// Solo se abren: nunca se envían formularios.
const ADMIN = [
    { nombre: 'admin-resumen', url: '/Admin' },
    { nombre: 'admin-productos', url: '/Admin/Products' },
    { nombre: 'admin-producto-nuevo', url: '/Admin/ProductCreate' },
    { nombre: 'admin-rutas', url: '/Admin/Services' },
    { nombre: 'admin-ruta-nueva', url: '/Admin/ServiceCreate' },
    { nombre: 'admin-pedidos', url: '/Admin/Orders' },
    { nombre: 'admin-reservas', url: '/Admin/Reservations' },
    { nombre: 'admin-guias', url: '/Admin/Guides' },
    { nombre: 'admin-transportes', url: '/Admin/Transports' },
    { nombre: 'admin-reportes', url: '/Admin/Reports' },
    { nombre: 'admin-reporte-ingresos', url: '/Admin/ReportIncome' }
];

// Filtro opcional para probar solo una sección: SOLO=productos npx playwright test
const filtrar = lista => lista.filter(p => !process.env.SOLO || p.nombre.includes(process.env.SOLO));

async function revisar(page, pagina, pantalla) {
    const errores = [];
    page.on('pageerror', error => errores.push(error.message));

    const url = await abrir(page, pagina);
    test.skip(!url, 'No hay datos para abrir esta página');

    await recorrerPagina(page);
    // La captura va antes de las comprobaciones: así existe aunque la prueba falle.
    await page.screenshot({ path: `capturas/${pantalla.nombre}/${pagina.nombre}.png`, fullPage: true });

    const { desborde, culpables, overflowOculto } = await medirDesborde(page);
    expect(overflowOculto, 'No se permite overflow-x: hidden en html o body').toBe(false);
    expect(desborde, `Scroll horizontal de ${desborde}px. Causas: ${culpables.join(', ')}`).toBeLessThanOrEqual(1);
    expect(errores, 'Errores de JavaScript en la página').toEqual([]);
}

for (const pantalla of PANTALLAS) {
    test.describe(pantalla.nombre, () => {
        test.use({
            viewport: { width: pantalla.width, height: pantalla.height },
            isMobile: pantalla.tactil,
            hasTouch: pantalla.tactil
        });

        for (const pagina of filtrar(PUBLICAS)) {
            test(pagina.nombre, async ({ page }) => revisar(page, pagina, pantalla));
        }

        test.describe('con sesión', () => {
            test.skip(!haySesion(), 'Define ADMIN_EMAIL y ADMIN_PASSWORD para probar estas páginas');
            test.use({ storageState: SESION });

            for (const pagina of filtrar([...CLIENTE, ...ADMIN])) {
                test(pagina.nombre, async ({ page }) => revisar(page, pagina, pantalla));
            }
        });
    });
}
