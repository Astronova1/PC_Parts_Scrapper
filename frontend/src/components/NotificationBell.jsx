import React, { useState, useRef, useEffect } from 'react';
import { useNotifications } from '../context/NotificationContext';
import './NotificationBell.css';

const NotificationBell = () => {
  const { notifications, unreadCount, markAsRead } = useNotifications();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef(null); 

  const toggleDropdown = (e) => {
    e.stopPropagation(); 
    setIsOpen((prev) => !prev);
  };

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    } else {
      document.removeEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape') {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEsc);
    }
    return () => {
      document.removeEventListener('keydown', handleEsc);
    };
  }, [isOpen]);

  return (
    <div className="notification-container" ref={containerRef}>
      <button className="notification-bell" onClick={toggleDropdown}>
        🔔
        {unreadCount > 0 && (
          <span className="notification-badge">{unreadCount}</span>
        )}
      </button>

      {isOpen && (
        <div className="notification-dropdown">
          <h4>Notifications</h4>
          {notifications.length === 0 ? (
            <p className="no-notifications">No new notifications</p>
          ) : (
            notifications.map((n) => (
              <div key={n.id} className="notification-item">
                <strong>{n.title}</strong>
                <p>{n.message}</p>
                <button onClick={() => markAsRead(n.id)}>Mark as read</button>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;