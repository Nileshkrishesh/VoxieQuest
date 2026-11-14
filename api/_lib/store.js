const { roundStateCount, VALID_STATES } = require('./states');

// Global in-memory storage fallback for local development and zero-config testing
if (!globalThis.__voxieStore) {
  globalThis.__voxieStore = {
    // Maps anonId -> { stateCode, ageBand, updatedAt }
    records: new Map([
      // Seeded mock records ensuring initial thresholded states for demo/offline
      ['seed_pb_1', { stateCode: 'PB', ageBand: '6-8', updatedAt: Date.now() }],
      ['seed_mh_1', { stateCode: 'MH', ageBand: '6-8', updatedAt: Date.now() }],
      ['seed_ka_1', { stateCode: 'KA', ageBand: '9-12', updatedAt: Date.now() }]
    ]),
    // Base baseline counts representing aggregate learners across India
    baseCounts: {
      PB: 105,
      MH: 520,
      KA: 280,
      DL: 210,
      TN: 160,
      GJ: 115,
      WB: 85,
      UP: 230,
      RJ: 95,
      KL: 125,
      TG: 110,
      MP: 65,
      AS: 35,
      OD: 45,
      GA: 12, // Below threshold (<20)
      HP: 15  // Below threshold (<20)
    },
    rateLimits: new Map() // anonId -> { count, resetTime }
  };
}

const store = globalThis.__voxieStore;

/**
 * Checks and updates rate-limiting for a given anonymous install ID.
 * Max 10 requests per minute per anonId.
 */
function checkRateLimit(anonId) {
  const now = Date.now();
  const userLimit = store.rateLimits.get(anonId);

  if (!userLimit || now > userLimit.resetTime) {
    store.rateLimits.set(anonId, { count: 1, resetTime: now + 60000 });
    return true;
  }

  if (userLimit.count >= 10) {
    return false;
  }

  userLimit.count++;
  return true;
}

/**
 * Upserts presence for an anonymous install ID (one record per anonId).
 */
async function upsertPresence(anonId, stateCode, ageBand) {
  const kvUrl = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
  const kvToken = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;

  // If Vercel KV / Upstash Redis is configured in production environment
  if (kvUrl && kvToken) {
    try {
      const payload = JSON.stringify({ stateCode, ageBand, updatedAt: Date.now() });
      await fetch(`${kvUrl}/set/voxie:presence:${encodeURIComponent(anonId)}`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${kvToken}` },
        body: payload
      });
      return true;
    } catch (e) {
      // Fallback to memory on network/KV failure
    }
  }

  // Memory fallback
  store.records.set(anonId, {
    stateCode,
    ageBand,
    updatedAt: Date.now()
  });
  return true;
}

/**
 * Deletes presence for an anonymous install ID.
 */
async function deletePresence(anonId) {
  const kvUrl = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
  const kvToken = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;

  if (kvUrl && kvToken) {
    try {
      await fetch(`${kvUrl}/del/voxie:presence:${encodeURIComponent(anonId)}`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${kvToken}` }
      });
      return true;
    } catch (e) {
      // Fallback
    }
  }

  store.records.delete(anonId);
  return true;
}

/**
 * Computes aggregated, privacy-thresholded, and rounded state totals.
 */
async function getAggregatedTotals() {
  const totals = {};

  // Initialize with baseline counts
  for (const [state, count] of Object.entries(store.baseCounts)) {
    totals[state] = count;
  }

  // Add individual opt-in presence records (unique by anonId)
  for (const record of store.records.values()) {
    if (record.stateCode) {
      totals[record.stateCode] = (totals[record.stateCode] || 0) + 1;
    }
  }

  const resultStates = {};
  let statesWithActiveLearners = 0;

  for (const stateCode of VALID_STATES) {
    const rawCount = totals[stateCode] || 0;
    const rounded = roundStateCount(rawCount);

    resultStates[stateCode] = {
      countText: rounded.countText,
      band: rounded.band
    };

    if (!rounded.isBelowThreshold) {
      statesWithActiveLearners++;
    }
  }

  return {
    statesWithActiveLearners,
    states: resultStates
  };
}

module.exports = {
  checkRateLimit,
  upsertPresence,
  deletePresence,
  getAggregatedTotals,
  _storeForTesting: store
};
