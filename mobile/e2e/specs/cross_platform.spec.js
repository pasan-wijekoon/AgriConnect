import { expect } from 'chai';
import { login, request } from '../helpers/api.js';
import { step } from '../helpers/screenshot.js';

const TEST_ID = 'XP-M-02';

async function act(description, action) {
  await action();
  await step(browser, TEST_ID, description);
}

async function check(description, assertion) {
  let failure;
  try {
    await assertion();
  } catch (error) {
    failure = error;
  }
  await step(browser, TEST_ID, `verify_${description}`);
  if (failure) throw failure;
}

describe('AgriConnect web/API listing on mobile', function () {
  // XP-M-02
  it('[XP-M-02] shows an API-created listing with the same price, quantity, and grade', async function () {
    await step(browser, TEST_ID, 'app_launched');
    const password = process.env.TEST_ACCOUNT_PASSWORD;
    const email = process.env.TEST_BUYER_EMAIL || 'buyer@agriconnect.lk';
    const buyer = await login(email, password);
    await step(browser, TEST_ID, 'buyer_api_login_verified');
    const result = await request(
      '/api/listings?status=Published&page=1&pageSize=50&sortBy=date&sortDir=desc',
      { token: buyer.token },
    );
    const listing = result.items.find(
      (item) =>
        item.minPrice != null &&
        Number(item.availableQuantity ?? item.quantity) > 0,
    );
    await check('published_listing_fixture_found', async () => {
      expect(
        listing,
        'Seed a Published listing with a floor price and available quantity.',
      ).to.exist;
    });
    await step(browser, TEST_ID, 'api_listing_fixture_verified');

    await act('open_buyer_login', async () =>
      browser.flutterByText$('Sign in').click(),
    );
    await act('enter_buyer_email', async () =>
      browser.flutterByValueKey$('email_field').setValue(email),
    );
    await act('enter_buyer_password', async () =>
      browser.flutterByValueKey$('password_field').setValue(password),
    );
    await act('submit_buyer_login', async () =>
      browser.flutterByValueKey$('login_button').click(),
    );
    await check('buyer_home_loaded', async () => {
      expect(await browser.flutterByTextContaining$('Good ').getText()).to
        .match(/Good (morning|afternoon|evening),/);
    });
    await act('open_market_tab', async () =>
      browser.flutterByValueKey$('tab_market').click(),
    );
    await act('open_fixture_listing', async () =>
      browser.flutterByText$(listing.cropName).click(),
    );
    const page = await browser.getPageSource();
    await check('api_and_mobile_listing_values_match', async () => {
      expect(page).to.include(listing.cropName);
      expect(page).to.include(String(listing.quantity));
      expect(page).to.include(listing.claimedGrade);
      expect(page).to.include(String(listing.minPrice));
    });
  });
});
