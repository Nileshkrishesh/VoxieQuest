// Whitelist of 28 Indian States and 8 Union Territories (ISO 3166-2:IN codes)
const VALID_STATES = new Set([
  'AP', 'AR', 'AS', 'BR', 'CT', 'GA', 'GJ', 'HR', 'HP', 'JH',
  'KA', 'KL', 'MP', 'MH', 'MN', 'ML', 'MZ', 'NL', 'OD', 'PB',
  'RJ', 'SK', 'TN', 'TG', 'TR', 'UP', 'UT', 'WB',
  'AN', 'CH', 'DH', 'DL', 'JK', 'LA', 'LD', 'PY'
]);

const VALID_AGE_BANDS = new Set(['3-5', '6-8', '9-12', '13+']);

// Privacy threshold: states with fewer than 20 players show "Just starting here!"
const MIN_PLAYERS_THRESHOLD = 20;

function normalizeStateCode(code) {
  if (!code || typeof code !== 'string') return null;
  let clean = code.trim().toUpperCase();
  if (clean.startsWith('IN-')) clean = clean.substring(3);
  return VALID_STATES.has(clean) ? clean : null;
}

function isValidAgeBand(band) {
  return Boolean(band && typeof band === 'string' && VALID_AGE_BANDS.has(band.trim()));
}

function isValidAnonId(id) {
  if (!id || typeof id !== 'string') return false;
  const clean = id.trim();
  // Validates standard 32-hex or 36-char hyphenated UUID string
  return /^[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12}$/.test(clean);
}

function roundStateCount(rawCount) {
  const count = Math.max(0, parseInt(rawCount, 10) || 0);

  // Requirement 1 & 5: Minimum threshold of 20 players
  if (count < MIN_PLAYERS_THRESHOLD) {
    return {
      countText: 'Just starting here!',
      band: 'JustStarting',
      isBelowThreshold: true
    };
  }

  // Rounded count brackets (never exposes live numbers or exact counts)
  let countText;
  let band = 'Growing';

  if (count >= 1000) {
    countText = '1,000+';
    band = 'Busy';
  } else if (count >= 500) {
    countText = '500+';
    band = 'Busy';
  } else if (count >= 250) {
    countText = '250+';
    band = 'Busy';
  } else if (count >= 100) {
    countText = '100+';
    band = 'Growing';
  } else if (count >= 50) {
    countText = '50+';
    band = 'Growing';
  } else {
    countText = '20+';
    band = 'Growing';
  }

  return {
    countText,
    band,
    isBelowThreshold: false
  };
}

module.exports = {
  VALID_STATES,
  VALID_AGE_BANDS,
  MIN_PLAYERS_THRESHOLD,
  normalizeStateCode,
  isValidAgeBand,
  isValidAnonId,
  roundStateCount
};
