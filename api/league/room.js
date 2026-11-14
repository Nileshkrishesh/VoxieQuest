const { isValidRoomCode, isValidWeekId, getRoomLeaderboard, createRoom } = require('../_lib/leagueStore');

module.exports = async function handler(req, res) {
  // CORS configuration
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  // GET: Fetch Top 5 Leaderboard
  if (req.method === 'GET') {
    const { roomCode, weekId, anonId } = req.query || {};

    if (!isValidRoomCode(roomCode)) {
      return res.status(400).json({ error: 'Invalid room code (must be 6 alphanumeric characters)' });
    }

    if (!isValidWeekId(weekId)) {
      return res.status(400).json({ error: 'Invalid weekId format (e.g. 2026-W46)' });
    }

    try {
      const summary = await getRoomLeaderboard(roomCode, weekId, anonId);
      // Cache-Control: room updates frequently during active play
      res.setHeader('Cache-Control', 'no-cache, no-store, must-revalidate');
      return res.status(200).json(summary);
    } catch (err) {
      return res.status(500).json({ error: 'Failed to retrieve room leaderboard' });
    }
  }

  // POST: Create New Private Room (Family or Classroom)
  if (req.method === 'POST') {
    const { isTeacherMode, classGoal } = req.body || {};

    try {
      const code = await createRoom(isTeacherMode, classGoal);
      return res.status(201).json({
        success: true,
        roomCode: code,
        message: 'Room created successfully'
      });
    } catch (err) {
      return res.status(500).json({ error: 'Failed to generate room code' });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
};
