import { chromium } from "playwright";
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
await page.goto('http://localhost:3000/login', { waitUntil:'networkidle' });
console.log(await page.locator('body').innerText());
await browser.close();
