const { getAggregatedTotals } = require('./_lib/store');

module.exports = async function handler(req, res) {
  // CORS Headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  // Cache aggregated totals for 24 hours (86400 seconds) with 12 hour stale-while-revalidate
  res.setHeader('Cache-Control', 'public, s-maxage=86400, stale-while-revalidate=43200');

  // Strict Privacy: Never log IP addresses
  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  if (req.method !== 'GET') {
    return res.status(405).json({
      success: false,
      error: 'Method not allowed.'
    });
  }

  try {
    const { statesWithActiveLearners, states } = await getAggregatedTotals();

    return res.status(200).json({
      success: true,
      timestamp: new Date().toISOString(),
      statesWithActiveLearners,
      states,
      isOfflineFallback: false
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: 'Unable to calculate aggregate map totals.'
    });
  }
};
