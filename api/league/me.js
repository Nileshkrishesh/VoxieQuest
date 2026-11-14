const { deleteUserScores } = require('../_lib/leagueStore');

module.exports = async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  if (req.method !== 'DELETE') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { anonId } = req.body || {};

  if (!anonId || typeof anonId !== 'string') {
    return res.status(400).json({ error: 'Missing or invalid anonId' });
  }

  try {
    await deleteUserScores(anonId);
    return res.status(200).json({
      success: true,
      message: 'All weekly league scores purged successfully'
    });
  } catch (err) {
    return res.status(500).json({ error: 'Failed to purge user league scores' });
  }
};
