// Inicia sesión una vez y guarda la cookie para las pruebas de páginas con sesión.
// Sin ADMIN_EMAIL y ADMIN_PASSWORD no hace nada y esas pruebas se omiten.
const fs = require('fs');
const path = require('path');
const { chromium } = require('@playwright/test');
const { SESION, haySesion } = require('./helpers');

module.exports = async config => {
    if (!haySesion()) return;

    const baseURL = config.projects[0].use.baseURL;
    const browser = await chromium.launch();
    const page = await browser.newPage();

    await page.goto(new URL('/Account/Login', baseURL).toString());
    await page.fill('input[name="Email"]', process.env.ADMIN_EMAIL);
    await page.fill('input[name="Password"]', process.env.ADMIN_PASSWORD);
    // El login tiene otros formularios (Google, GitHub): se envía el del correo.
    await page.click('form:has(input[name="Password"]) button[type="submit"]');
    await page.waitForURL(url => !url.pathname.includes('/Account/Login'), { timeout: 15000 });

    fs.mkdirSync(path.dirname(SESION), { recursive: true });
    await page.context().storageState({ path: SESION });
    await browser.close();
};
