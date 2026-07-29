/**
 * AuthService — Singleton
 * Handles user registration, login, logout, and session management
 * against the ASP.NET Core /api/User endpoints (cookie-based auth).
 */
import { User } from '../models/User';
import { apiFetch, ApiError } from '../api';

function toUser(dto) {
  return new User({
    userId:   dto.id,
    fullName: dto.name,
    email:    dto.email,
    role:     (dto.role || 'user').toLowerCase(),
  });
}

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
    if (!email || !password) {
      throw new Error('Email and password are required.');
    }
    try {
      const dto = await apiFetch('/api/User/Login', {
        method: 'POST',
        body: { Email: email, Password: password },
      });
      this.#currentUser = toUser(dto);
      return this.#currentUser;
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) {
        throw new Error('Email/Password Incorrect');
      }
      throw e;
    }
  }

  /**
   * Registers a new user account, then logs in immediately.
   * @param {string} fullName
   * @param {string} email
   * @param {string} password
   * @returns {Promise<User>}
   */
  async register(fullName, email, password) {
    if (!fullName || !email || !password) {
      throw new Error('All fields are required.');
    }
    if (password.length < 8) {
      throw new Error('Password must be at least 8 characters.');
    }
    try {
      await apiFetch('/api/User/Create', {
        method: 'POST',
        body: { Name: fullName, Email: email, Password: password, role: 'user' },
      });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        throw new Error('An account with this email already exists.');
      }
      throw e;
    }
    return this.login(email, password);
  }

  /** Clears the current session, both locally and on the backend. */
  async logout() {
    this.#currentUser = null;
    try {
      await apiFetch('/api/User/Logout');
    } catch {
      // Cookie may already be gone — nothing more to do locally.
    }
  }

  /**
   * Asks the backend who the current session belongs to.
   * Used to restore login state after a page reload.
   * @returns {Promise<User|null>}
   */
  async fetchCurrentUser() {
    try {
      const dto = await apiFetch('/api/User/Me');
      this.#currentUser = toUser(dto);
      return this.#currentUser;
    } catch {
      this.#currentUser = null;
      return null;
    }
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
