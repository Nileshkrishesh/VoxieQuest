const assert = require('assert');
const {
  VALID_STATES,
  normalizeStateCode,
  isValidAgeBand,
  isValidAnonId,
  roundStateCount,
  MIN_PLAYERS_THRESHOLD
} = require('../api/_lib/states');
const {
  checkRateLimit,
  upsertPresence,
  deletePresence,
  getAggregatedTotals,
  _storeForTesting
} = require('../api/_lib/store');

async function runTests() {
  console.log('=== [Voxie Across India] Running Serverless API Tests ===\n');

  // Test 1: Whitelist & State Normalization
  console.log('Test 1: Valid and invalid state codes...');
  assert.strictEqual(normalizeStateCode('PB'), 'PB', 'Punjab should normalize to PB');
  assert.strictEqual(normalizeStateCode('in-pb'), 'PB', 'in-pb should normalize to PB');
  assert.strictEqual(normalizeStateCode('MH'), 'MH', 'Maharashtra should be valid');
  assert.strictEqual(normalizeStateCode('XX'), null, 'XX must be rejected');
  assert.strictEqual(normalizeStateCode('California'), null, 'Non-Indian regions must be rejected');
  assert.strictEqual(normalizeStateCode(''), null, 'Empty code must be rejected');
  assert.strictEqual(VALID_STATES.size, 36, 'Should have exactly 28 states + 8 UTs (36 total)');
  console.log('  PASS: Whitelist validation passed.\n');

  // Test 2: Age Band Validation
  console.log('Test 2: Age band validation...');
  assert.strictEqual(isValidAgeBand('3-5'), true);
  assert.strictEqual(isValidAgeBand('6-8'), true);
  assert.strictEqual(isValidAgeBand('9-12'), true);
  assert.strictEqual(isValidAgeBand('13+'), true);
  assert.strictEqual(isValidAgeBand('adult'), false);
  assert.strictEqual(isValidAgeBand(''), false);
  console.log('  PASS: Age band validation passed.\n');

  // Test 3: Anonymous ID (UUID) Validation
  console.log('Test 3: Anonymous Install ID validation...');
  assert.strictEqual(isValidAnonId('550e8400-e29b-41d4-a716-446655440000'), true);
  assert.strictEqual(isValidAnonId('550e8400e29b41d4a716446655440000'), true);
  assert.strictEqual(isValidAnonId('user_john_doe'), false, 'Non-UUID format must be rejected');
  assert.strictEqual(isValidAnonId('123'), false, 'Short IDs must be rejected');
  console.log('  PASS: Anonymous ID validation passed.\n');

  // Test 4: Threshold (<20 players) Hiding
  console.log('Test 4: Privacy threshold (<20 players)...');
  const belowThreshold = roundStateCount(15);
  assert.strictEqual(belowThreshold.isBelowThreshold, true);
  assert.strictEqual(belowThreshold.countText, 'Just starting here!');
  assert.strictEqual(belowThreshold.band, 'JustStarting');

  const zeroPlayers = roundStateCount(0);
  assert.strictEqual(zeroPlayers.countText, 'Just starting here!');
  console.log('  PASS: States below threshold are hidden correctly.\n');

  // Test 5: Rounded Counts
  console.log('Test 5: Count rounding brackets...');
  assert.strictEqual(roundStateCount(20).countText, '20+');
  assert.strictEqual(roundStateCount(35).countText, '20+');
  assert.strictEqual(roundStateCount(55).countText, '50+');
  assert.strictEqual(roundStateCount(140).countText, '100+');
  assert.strictEqual(roundStateCount(280).countText, '250+');
  assert.strictEqual(roundStateCount(750).countText, '500+');
  assert.strictEqual(roundStateCount(1200).countText, '1,000+');
  console.log('  PASS: Counts rounded correctly without exposing exact numbers.\n');

  // Test 6: One anonId can only be counted once (Upsert test)
  console.log('Test 6: Upsert (Single count per anonId)...');
  const testAnonId = 'e7b1a2c3-4d5e-6f7a-8b9c-0d1e2f3a4b5c';

  await upsertPresence(testAnonId, 'PB', '6-8');
  assert.strictEqual(_storeForTesting.records.get(testAnonId)?.stateCode, 'PB');

  // Update same anonId to MH
  await upsertPresence(testAnonId, 'MH', '6-8');
  assert.strictEqual(_storeForTesting.records.get(testAnonId)?.stateCode, 'MH', 'Record must be updated, not duplicated');

  // Verify only 1 entry for this anonId
  let countOfId = 0;
  for (const [id] of _storeForTesting.records) {
    if (id === testAnonId) countOfId++;
  }
  assert.strictEqual(countOfId, 1, 'AnonId must only have exactly 1 record');
  console.log('  PASS: Upsert preserves 1 record per install ID.\n');

  // Test 7: Opt-out deletes the record
  console.log('Test 7: Opt-out deletes record...');
  await deletePresence(testAnonId);
  assert.strictEqual(_storeForTesting.records.has(testAnonId), false, 'Record must be completely removed on delete');
  console.log('  PASS: Opt-out permanently removes presence.\n');

  // Test 8: Aggregated Totals Output
  console.log('Test 8: Aggregated Totals...');
  const totals = await getAggregatedTotals();
  assert.strictEqual(typeof totals.statesWithActiveLearners, 'number');
  assert.strictEqual(totals.statesWithActiveLearners >= 14, true, 'Seeded states should show active learners');
  assert.strictEqual(totals.states['PB']?.band, 'Growing');
  assert.strictEqual(totals.states['GA']?.countText, 'Just starting here!');
  console.log('  PASS: Aggregated totals output matches contract.\n');

  console.log('=== ALL 8 SERVERLESS API TESTS PASSED! ===');
}

runTests().catch(err => {
  console.error('API Test Failed:', err);
  process.exit(1);
});
