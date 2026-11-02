import { useEffect, useRef, useState } from 'react';
import { useAuth } from '@/core/auth/AuthContext';
import { dailyStatusService } from '@/core/services/services';
import type { DailyStatus } from '@/core/models/types';
import { useNavigate } from 'react-router-dom';

interface StatusActivity {
  id: number;
  statusId: number;
  memberName: string;
  action: 'logged' | 'updated';
  taskTitle: string;
  status: string;
  timeFormatted: string;
  timestamp: Date;
  dateStr?: string;
}

export function NotificationBell() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [activities, setActivities] = useState<StatusActivity[]>([]);
  const [lastReadTimestamp, setLastReadTimestamp] = useState<number>(() => {
    const saved = localStorage.getItem(`wf_notif_read_${user?.id}`);
    return saved ? Number(saved) : 0;
  });
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Notifications are relevant for Team Leads, Managers, HR, and Admins
  const isLeadOrManager =
    user?.role === 'TeamLead' ||
    user?.role === 'Manager' ||
    user?.role === 'Administrator' ||
    user?.role === 'HR';

  const fetchActivities = async () => {
    if (!isLeadOrManager) return;
    try {
      // Fetch latest 30 daily statuses within the user's accessible scope
      const res = await dailyStatusService.list({
        pageSize: 30,
        page: 1,
      });

      const items: StatusActivity[] = (res.items || []).map((ds: DailyStatus) => {
        const created = new Date(ds.createdAt);
        const updated = new Date(ds.updatedAt);
        // If updated is distinctly later than created (by > 2 seconds), consider it an edit
        const isEdit = updated.getTime() - created.getTime() > 2000;
        const eventTime = isEdit ? updated : created;

        const timeStr = eventTime.toLocaleTimeString([], {
          hour: '2-digit',
          minute: '2-digit',
          hour12: true,
        });

        const dateStr = eventTime.toLocaleDateString([], {
          month: 'short',
          day: 'numeric',
        });

        return {
          id: ds.id,
          statusId: ds.id,
          memberName: ds.employeeName || 'Team Member',
          action: isEdit ? 'updated' : 'logged',
          taskTitle: ds.taskActivity,
          status: ds.status,
          timeFormatted: `${dateStr} at ${timeStr}`,
          timestamp: eventTime,
          dateStr: ds.date ? ds.date.split('T')[0] : '',
        };
      });

      // Sort latest first
      items.sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime());
      setActivities(items);
    } catch {
      // Background poll failure handled gracefully
    }
  };

  useEffect(() => {
    fetchActivities();
    // Poll every 30 seconds for live updates
    const interval = setInterval(fetchActivities, 30000);
    return () => clearInterval(interval);
  }, [user?.id, isLeadOrManager]);

  // Click outside to close
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    if (open) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open]);

  if (!isLeadOrManager) {
    return null;
  }

  const unreadCount = activities.filter(
    (a) => a.timestamp.getTime() > lastReadTimestamp
  ).length;

  const handleToggle = () => {
    if (!open) {
      const now = Date.now();
      setLastReadTimestamp(now);
      localStorage.setItem(`wf_notif_read_${user?.id}`, String(now));
    }
    setOpen((prev) => !prev);
  };

  const handleNotificationClick = (act: StatusActivity) => {
    setOpen(false);
    navigate(`/daily-status?search=${encodeURIComponent(act.memberName)}`);
  };

  const markAllRead = () => {
    const now = Date.now();
    setLastReadTimestamp(now);
    localStorage.setItem(`wf_notif_read_${user?.id}`, String(now));
  };

  return (
    <div className="notif-wrapper" ref={dropdownRef} style={{ position: 'relative' }}>
      <button
        type="button"
        className="notif-bell-btn"
        onClick={handleToggle}
        aria-label={`Notifications, ${unreadCount} unread`}
        title="Status Activity Notifications"
      >
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>

        {unreadCount > 0 && (
          <span className="notif-badge">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="notif-dropdown" role="region" aria-label="Notifications list">
          <div className="notif-dropdown__header">
            <div>
              <h3 className="notif-dropdown__title">Status Notifications</h3>
              <p className="notif-dropdown__subtitle">
                Updates from your team members
              </p>
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                className="notif-mark-read-btn"
                onClick={markAllRead}
              >
                Mark read
              </button>
            )}
          </div>

          <div className="notif-dropdown__list">
            {activities.length === 0 ? (
              <div className="notif-empty">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
                  <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
                  <path d="M13.73 21a2 2 0 0 1-3.46 0" />
                </svg>
                <p>No recent status updates</p>
              </div>
            ) : (
              activities.slice(0, 15).map((act) => {
                const isUnread = act.timestamp.getTime() > lastReadTimestamp;
                return (
                  <div
                    key={`${act.id}-${act.action}-${act.timestamp.getTime()}`}
                    className={`notif-item ${isUnread ? 'notif-item--unread' : ''}`}
                    onClick={() => handleNotificationClick(act)}
                    role="button"
                    tabIndex={0}
                  >
                    <div className="notif-item__icon-wrap">
                      <span className={`notif-action-dot notif-action-dot--${act.action}`} />
                    </div>
                    <div className="notif-item__content">
                      <p className="notif-item__text">
                        <strong>{act.memberName}</strong>{' '}
                        {act.action === 'updated' ? 'updated the status' : 'logged a daily status'}:
                      </p>
                      <p className="notif-item__task" title={act.taskTitle}>
                        "{act.taskTitle}"
                      </p>
                      <div className="notif-item__meta">
                        <span className="notif-item__badge">{act.status}</span>
                        <span className="notif-item__time">{act.timeFormatted}</span>
                      </div>
                    </div>
                  </div>
                );
              })
            )}
          </div>

          <div className="notif-dropdown__footer">
            <button
              type="button"
              className="notif-dropdown__view-all"
              onClick={() => {
                setOpen(false);
                navigate('/daily-status');
              }}
            >
              View all daily statuses &rarr;
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
