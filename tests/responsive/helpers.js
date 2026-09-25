// Utilidades compartidas por las pruebas responsive.
const path = require('path');
const { expect } = require('@playwright/test');

// Cookie de sesión que guarda global-setup.js (nunca se sube a git).
const SESION = path.join(__dirname, '.auth', 'sesion.json');

// Las páginas con sesión solo se prueban si hay credenciales en variables de entorno.
const haySesion = () => Boolean(process.env.ADMIN_EMAIL && process.env.ADMIN_PASSWORD);

// Tamaños de pantalla (ancho x alto). "tactil" emula un teléfono o una tablet.
const PANTALLAS = [
    { nombre: '320-movil-chico', width: 320, height: 568, tactil: true },
    { nombre: '390-movil', width: 390, height: 844, tactil: true },
    { nombre: '844-movil-horizontal', width: 844, height: 390, tactil: true },
    { nombre: '768-tablet', width: 768, height: 1024, tactil: true },
    { nombre: '1024-tablet-horizontal', width: 1024, height: 768, tactil: true },
    { nombre: '1280-laptop', width: 1280, height: 800, tactil: false },
    { nombre: '1920-escritorio', width: 1920, height: 1080, tactil: false }
];

// Abre una página. Las de detalle se buscan desde su listado, porque los Id cambian.
// Devuelve null si no hay datos para abrirla (ej. no hay productos cargados).
async function abrir(page, pagina) {
    let url = pagina.url;

    if (pagina.desde) {
        await page.goto(pagina.desde);
        const enlace = page.locator(pagina.enlace).first();
        if (await enlace.count() === 0) return null;
        url = await enlace.getAttribute('href');
    }

    const respuesta = await page.goto(url, { waitUntil: 'load' });
    expect(respuesta.status(), `${url} respondió con error`).toBeLessThan(400);
    return url;
}

// Baja por toda la página para que se disparen las animaciones de aparición
// (IntersectionObserver) antes de tomar la captura.
async function recorrerPagina(page) {
    await page.evaluate(async () => {
        const paso = Math.max(200, window.innerHeight / 2);
        for (let y = 0; y < document.documentElement.scrollHeight; y += paso) {
            window.scrollTo(0, y);
            await new Promise(resolve => setTimeout(resolve, 80));
        }
        window.scrollTo(0, 0);
    });
    await page.waitForTimeout(400);
}

// Mide el scroll horizontal (0 = correcto) y lista los elementos que lo causan.
async function medirDesborde(page) {
    return page.evaluate(() => {
        const ancho = document.documentElement.clientWidth;
        const desborde = document.documentElement.scrollWidth - ancho;

        // Un elemento no causa scroll si algún contenedor suyo lo recorta o lo desplaza.
        const contenido = el => {
            for (let p = el.parentElement; p && p !== document.body; p = p.parentElement) {
                const estilo = getComputedStyle(p);
                if (estilo.overflowX !== 'visible' || estilo.clipPath !== 'none') return true;
            }
            return false;
        };

        const culpables = [];
        if (desborde > 1) {
            document.querySelectorAll('body *').forEach(el => {
                const r = el.getBoundingClientRect();
                if (r.width > 0 && r.right > ancho + 1 && !contenido(el)) {
                    const clase = typeof el.className === 'string' ? el.className.trim().split(/\s+/)[0] : '';
                    culpables.push(el.tagName.toLowerCase() + (clase ? '.' + clase : '') + ' (termina en ' + Math.round(r.right) + 'px)');
                }
            });
        }

        return {
            desborde,
            culpables: culpables.slice(0, 10),
            overflowOculto: getComputedStyle(document.body).overflowX === 'hidden'
                || getComputedStyle(document.documentElement).overflowX === 'hidden'
        };
    });
}

module.exports = { SESION, haySesion, PANTALLAS, abrir, recorrerPagina, medirDesborde };
