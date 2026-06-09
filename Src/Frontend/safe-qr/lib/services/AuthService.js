/**
 * AuthService — Singleton
 * Handles user registration, login, logout, and session management.
 * In production this would call the ASP.NET Core /api/auth endpoints.
 */
import { User } from '../models/User';

export class AuthService {
  static #instance = null;
  #currentUser     = null;

  static getInstance() {
    if (!AuthService.#instance) {
      AuthService.#instance = new AuthService();
    }
    return AuthService.#instance;
  }

  /**
   * Authenticates a user with email and password.
   * @param {string} email
   * @param {string} password
   * @returns {Promise<User>}
   */
  async login(email, password) {
    await new Promise(r => setTimeout(r, 900));
    if (!email || !password) {
      throw new Error('Email and password are required.');
    }
    // TODO: replace with POST /api/auth/login
    this.#currentUser = new User({
      userId:   'u-' + Math.random().toString(36).slice(2),
      fullName: 'E-Ken Wee Yi Zhe',
      email,
      role: 'user',
    });
    return this.#currentUser;
  }

  /**
   * Registers a new user account.
   * @param {string} fullName
   * @param {string} email
   * @param {string} password
   * @returns {Promise<User>}
   */
  async register(fullName, email, password) {
    await new Promise(r => setTimeout(r, 900));
    if (!fullName || !email || !password) {
      throw new Error('All fields are required.');
    }
    if (password.length < 8) {
      throw new Error('Password must be at least 8 characters.');
    }
    // TODO: replace with POST /api/auth/register
    this.#currentUser = new User({
      userId:   'u-' + Math.random().toString(36).slice(2),
      fullName,
      email,
      role: 'user',
    });
    return this.#currentUser;
  }

  /** Clears the current session. */
  logout() {
    this.#currentUser = null;
  }

  /** Returns the currently authenticated user, or null. */
  getCurrentUser() {
    return this.#currentUser;
  }

  /** Returns true if a user is currently authenticated. */
  isAuthenticated() {
    return this.#currentUser !== null;
  }
}
