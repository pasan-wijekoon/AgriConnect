import { expect } from 'chai';
import { login, request } from '../helpers/api.js';
import { step } from '../helpers/screenshot.js';

const TEST_ID = 'XP-M-01';
const password = process.env.TEST_ACCOUNT_PASSWORD;
const farmerEmail = process.env.TEST_FARMER_EMAIL || 'farmer@agriconnect.lk';
const buyerEmail = process.env.TEST_BUYER_EMAIL || 'buyer@agriconnect.lk';
const officerEmail = process.env.TEST_OFFICER_EMAIL || 'officer@agriconnect.lk';

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

async function signIn(email, currentPassword, account) {
  await act(`open_sign_in_${account}`, async () =>
    browser.flutterByText$('Sign in').click(),
  );
  await act(`enter_${account}_email`, async () =>
    browser.flutterByValueKey$('email_field').setValue(email),
  );
  await act(`enter_${account}_password`, async () =>
    browser.flutterByValueKey$('password_field').setValue(currentPassword),
  );
  await act(`submit_${account}_sign_in`, async () =>
    browser.flutterByValueKey$('login_button').click(),
  );
  await check(`${account}_home_loaded`, async () => {
    expect(await browser.flutterByTextContaining$('Good ').getText()).to.match(
      /Good (morning|afternoon|evening),/,
    );
  });
}

describe('AgriConnect mobile full workflow', function () {
  // XP-M-01
  it('[XP-M-01] creates, inspects, publishes, and orders produce across the real API and mobile UI', async function () {
    await step(browser, TEST_ID, 'app_launched');
    const farmer = await login(farmerEmail, password);
    await step(browser, TEST_ID, 'farmer_api_login_verified');

    await signIn(farmerEmail, password, 'farmer');
    await act('open_farmer_listings', async () =>
      browser.flutterByValueKey$('tab_listings').click(),
    );
    await act('open_new_listing_form', async () =>
      browser.flutterByText$('New listing').click(),
    );
    await check('create_listing_form_loaded', async () => {
      expect(
        await browser.flutterByText$('New Produce Listing (FR3)').getText(),
      ).to.equal('New Produce Listing (FR3)');
    });
    await act('enter_listing_quantity', async () =>
      browser.flutterByType$('TextFormField').setValue('5'),
    );
    await act('submit_listing', async () =>
      browser.flutterByValueKey$('submitListingButton').click(),
    );
    await check('listing_detail_opened', async () => {
      expect(
        await browser.flutterByText$('Listing Specifications').getText(),
      ).to.equal('Listing Specifications');
    });

    const ownListings = await request('/api/listings/my-listings', {
      token: farmer.token,
    });
    const listing = ownListings
      .filter((item) => item.status === 'PendingApproval')
      .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0];
    await check('pending_listing_persisted', async () => {
      expect(listing, 'Farmer listing was not persisted as PendingApproval').to
        .exist;
    });
    await step(browser, TEST_ID, 'verify_pending_listing_persisted_in_api');

    const officer = await login(officerEmail, password);
    await step(browser, TEST_ID, 'officer_api_login_verified');
    const inspection = await request('/api/inspections', {
      token: officer.token,
      method: 'POST',
      body: {
        listingId: listing.id,
        confirmedGrade: listing.claimedGrade,
        notes: 'Appium XP-M-01 inspection test',
        photoUrls: [],
      },
    });
    await check('inspection_saved', async () => {
      expect(inspection.listingId).to.equal(listing.id);
    });
    await step(browser, TEST_ID, 'verify_inspection_persisted_in_api');
    const published = await request(
      `/api/listings/${listing.id}/publish`,
      { token: officer.token, method: 'POST' },
    );
    await check('officer_published_listing', async () => {
      expect(published.status).to.equal('Published');
    });
    await step(browser, TEST_ID, 'verify_publish_persisted_in_api');

    await act('open_farmer_account', async () =>
      browser.flutterByValueKey$('tab_account').click(),
    );
    await act('open_sign_out_confirmation', async () =>
      browser.flutterByText$('Sign out').click(),
    );
    await act('confirm_farmer_sign_out', async () =>
      browser.flutterByText$('Sign out').click(),
    );
    await signIn(buyerEmail, password, 'buyer');
    await act('open_buyer_market', async () =>
      browser.flutterByValueKey$('tab_market').click(),
    );
    await act('open_new_published_listing', async () =>
      browser.flutterByText$(listing.cropName).click(),
    );
    await check('published_listing_details_match_api', async () => {
      const details = await browser.getPageSource();
      expect(details).to.include(listing.cropName);
      expect(details).to.include(String(listing.quantity));
      expect(details).to.include(listing.claimedGrade);
    });
    await act('start_order', async () =>
      browser.flutterByText$(`Order ${listing.cropName}`).click(),
    );
    await act('enter_order_quantity', async () =>
      browser.flutterByValueKey$('order_quantity_field').setValue('1'),
    );
    await act('submit_order', async () =>
      browser.flutterByValueKey$('place_order_button').click(),
    );
    await check('order_confirmation_shown', async () => {
      expect(
        await browser
          .flutterByText$(
            'Order placed! Your stock is reserved while an officer reviews it.',
          )
          .getText(),
      ).to.include('Order placed!');
    });
    const buyer = await login(buyerEmail, password);
    const orders = await request('/api/orders?page=1&size=100', {
      token: buyer.token,
    });
    const order = orders.items.find((item) => item.listingId === listing.id);
    await check('order_persisted_in_api', async () => {
      expect(order, 'Buyer order was not persisted for the new listing').to.exist;
    });
    await step(browser, TEST_ID, 'verify_order_persisted_in_api');
  });
});
