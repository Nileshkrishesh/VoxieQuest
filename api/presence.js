const { normalizeStateCode, isValidAgeBand, isValidAnonId } = require('./_lib/states');
const { checkRateLimit, upsertPresence, deletePresence } = require('./_lib/store');

module.exports = async function handler(req, res) {
  // CORS Headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'POST, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  // Strict Privacy: Never log IP addresses or request headers
  // IP logging intentionally omitted to prevent device tracking

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  // --- DELETE /api/presence ---
  if (req.method === 'DELETE') {
    const anonId = req.query?.anonId || req.body?.anonId;

    if (!isValidAnonId(anonId)) {
      return res.status(400).json({
        success: false,
        error: 'Invalid or missing anonymous identifier.'
      });
    }

    await deletePresence(anonId);
    return res.status(200).json({
      success: true,
      message: 'Anonymous presence removed successfully.'
    });
  }

  // --- POST /api/presence ---
  if (req.method === 'POST') {
    let body = req.body;
    if (typeof body === 'string') {
      try {
        body = JSON.parse(body);
      } catch (e) {
        return res.status(400).json({ success: false, error: 'Malformed JSON payload.' });
      }
    }

    const { stateCode, ageBand, anonId } = body || {};

    // 1. Validate Anonymous Install ID
    if (!isValidAnonId(anonId)) {
      return res.status(400).json({
        success: false,
        error: 'Invalid anonymous ID format. Must be a valid UUID.'
      });
    }

    // 2. Rate-Limiting per anonId
    if (!checkRateLimit(anonId)) {
      return res.status(429).json({
        success: false,
        error: 'Too many requests. Please try again in a few moments.'
      });
    }

    // 3. Validate Indian State Code against whitelist
    const normalizedState = normalizeStateCode(stateCode);
    if (!normalizedState) {
      return res.status(400).json({
        success: false,
        error: 'Invalid stateCode. Must be a valid 2-letter Indian State or UT code.'
      });
    }

    // 4. Validate Developmental Age Band
    if (!isValidAgeBand(ageBand)) {
      return res.status(400).json({
        success: false,
        error: 'Invalid ageBand. Allowed values: 3-5, 6-8, 9-12, 13+.'
      });
    }

    // 5. Upsert single record per anonId (no duplicates)
    await upsertPresence(anonId, normalizedState, ageBand.trim());

    return res.status(200).json({
      success: true,
      message: 'Anonymous presence recorded successfully.'
    });
  }

  return res.status(405).json({
    success: false,
    error: 'Method not allowed.'
  });
};
