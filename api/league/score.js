const { checkRateLimit, isValidRoomCode, isValidWeekId, upsertScore } = require('../_lib/leagueStore');

module.exports = async function handler(req, res) {
  // CORS configuration
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { anonId, roomCode, weekId, points, displayName, avatarId } = req.body || {};

  // Input validation
  if (!anonId || typeof anonId !== 'string' || anonId.length > 64) {
    return res.status(400).json({ error: 'Invalid anonId' });
  }

  if (!isValidRoomCode(roomCode)) {
    return res.status(400).json({ error: 'Invalid room code (must be 6 alphanumeric characters)' });
  }

  if (!isValidWeekId(weekId)) {
    return res.status(400).json({ error: 'Invalid weekId format (e.g. 2026-W46)' });
  }

  const parsedPoints = parseInt(points, 10);
  if (isNaN(parsedPoints) || parsedPoints < 0 || parsedPoints > 1050) {
    return res.status(400).json({ error: 'Points must be between 0 and 1050' });
  }

  // Rate-limiting check
  if (!checkRateLimit(anonId)) {
    return res.status(429).json({ error: 'Too many requests. Please slow down.' });
  }

  try {
    const finalPoints = await upsertScore({
      anonId,
      roomCode,
      weekId,
      points: parsedPoints,
      displayName,
      avatarId
    });

    return res.status(200).json({
      success: true,
      points: finalPoints
    });
  } catch (err) {
    return res.status(500).json({ error: 'Internal error updating league score' });
  }
};
