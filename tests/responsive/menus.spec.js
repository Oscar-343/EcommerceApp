// Menú hamburguesa (todas las páginas públicas) y barra lateral del admin.
const { test, expect } = require('@playwright/test');
const { SESION, haySesion } = require('./helpers');

const MOVIL = { width: 390, height: 844 };
const ESCRITORIO = { width: 1280, height: 800 };

// Clic "afuera" sin riesgo de tocar un enlace de la página.
const clicAfuera = page => page.evaluate(() =>
    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true })));

test.describe('Menú hamburguesa', () => {
    for (const url of ['/', '/Products', '/Services', '/Account/Login']) {
        test(`funciona en ${url}`, async ({ page }) => {
            await page.setViewportSize(MOVIL);
            await page.goto(url);

            const menu = page.locator('#navMenu');
            const boton = page.locator('#navToggle');

            // Solo aplica a páginas con la navbar pública.
            test.skip(await boton.count() === 0, 'Esta página no usa la navbar pública');

            // Empieza cerrado.
            await expect(boton).toBeVisible();
            await expect(menu).toBeHidden();
            await expect(boton).toHaveAttribute('aria-expanded', 'false');

            // Abre al primer toque (el bug del Home: el primer toque no hacía nada).
            await boton.click();
            await expect(menu).toBeVisible();
            await expect(boton).toHaveAttribute('aria-expanded', 'true');
            await expect(page.locator('body')).toHaveClass(/nav-open/);

            // Opciones cómodas de tocar.
            for (const enlace of await menu.locator('a').all()) {
                const caja = await enlace.boundingBox();
                expect(caja.height, 'Opción del menú demasiado baja para tocar').toBeGreaterThanOrEqual(44);
            }

            // Cierra con el mismo botón.
            await boton.click();
            await expect(menu).toBeHidden();
            await expect(page.locator('body')).not.toHaveClass(/nav-open/);

            // Cierra con Escape.
            await boton.click();
            await page.keyboard.press('Escape');
            await expect(menu).toBeHidden();

            // Cierra con un clic afuera.
            await boton.click();
            await clicAfuera(page);
            await expect(menu).toBeHidden();

            // Abierto en móvil y luego la ventana crece: no debe quedar atascado.
            await boton.click();
            await page.setViewportSize(ESCRITORIO);
            await expect(menu).toBeVisible(); // menú horizontal de escritorio
            await expect(boton).toBeHidden();
            await expect(page.locator('body')).not.toHaveClass(/nav-open/);

            // Y al volver a móvil, empieza cerrado.
            await page.setViewportSize(MOVIL);
            await expect(menu).toBeHidden();
            await expect(boton).toHaveAttribute('aria-expanded', 'false');
        });
    }

    test('en escritorio el menú se ve sin botón', async ({ page }) => {
        await page.setViewportSize(ESCRITORIO);
        await page.goto('/');
        await expect(page.locator('#navMenu')).toBeVisible();
        await expect(page.locator('#navToggle')).toBeHidden();
    });
});

test.describe('Barra lateral del admin', () => {
    test.skip(!haySesion(), 'Define ADMIN_EMAIL y ADMIN_PASSWORD para probar el admin');
    test.use({ storageState: SESION });

    test('se abre y se cierra en móvil', async ({ page }) => {
        await page.setViewportSize(MOVIL);
        await page.goto('/Admin');

        const barra = page.locator('#adminSidebar');
        const boton = page.locator('#adminSidebarToggle');

        await expect(barra).not.toBeInViewport();
        await boton.click();
        await expect(barra).toBeInViewport();

        await page.keyboard.press('Escape');
        await expect(barra).not.toBeInViewport();

        await boton.click();
        await page.locator('#adminSidebarOverlay').click({ position: { x: MOVIL.width - 10, y: 400 } });
        await expect(barra).not.toBeInViewport();
    });

    test('en escritorio la barra está siempre visible', async ({ page }) => {
        await page.setViewportSize(ESCRITORIO);
        await page.goto('/Admin');
        await expect(page.locator('#adminSidebar')).toBeInViewport();
        await expect(page.locator('#adminSidebarToggle')).toBeHidden();
    });
});
