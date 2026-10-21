import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PERMISSIONS } from '@/core/auth/permissions';
import type { Organization } from '@/core/models/types';
import { organizationService } from '@/core/services/services';
import { renderAs } from '@/test/helpers';
import { OrganizationsPage } from './OrganizationsPage';

vi.mock('@/core/services/services', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/core/services/services')>();
  return { ...actual, organizationService: { ...actual.organizationService, list: vi.fn() } };
});

const org = (id: number, name: string): Organization => ({
  id,
  name,
  description: null,
  isActive: true,
  teamCount: 2,
  employeeCount: 5,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
});

describe('OrganizationsPage', () => {
  beforeEach(() => vi.mocked(organizationService.list).mockReset());

  it('shows whatever organizations the API returns, including ones the frontend has never heard of', async () => {
    vi.mocked(organizationService.list).mockResolvedValue([org(1, 'NNX'), org(2, '7NX'), org(3, 'HR & Operations'), org(4, 'ABC')]);
    renderAs(<OrganizationsPage />, [PERMISSIONS.Organization.View]);
    expect(await screen.findByRole('heading', { name: 'ABC' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'NNX' })).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /View organization/ })).toHaveLength(4);
  });

  it('hides create, edit and delete controls from a user without those permissions', async () => {
    vi.mocked(organizationService.list).mockResolvedValue([org(1, 'NNX')]);
    renderAs(<OrganizationsPage />, [PERMISSIONS.Organization.View]);
    await screen.findByRole('heading', { name: 'NNX' });
    expect(screen.queryByRole('button', { name: 'New organization' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Edit NNX/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Delete NNX/ })).not.toBeInTheDocument();
  });

  it('shows management controls when the permissions are held', async () => {
    vi.mocked(organizationService.list).mockResolvedValue([org(1, 'NNX')]);
    renderAs(<OrganizationsPage />, [
      PERMISSIONS.Organization.View,
      PERMISSIONS.Organization.Create,
      PERMISSIONS.Organization.Edit,
      PERMISSIONS.Organization.Delete,
    ]);
    await screen.findByRole('heading', { name: 'NNX' });
    expect(screen.getByRole('button', { name: 'New organization' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Edit NNX' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete NNX' })).toBeInTheDocument();
  });

  it('shows an empty state with guidance when there are no organizations', async () => {
    vi.mocked(organizationService.list).mockResolvedValue([]);
    renderAs(<OrganizationsPage />, [PERMISSIONS.Organization.View]);
    expect(await screen.findByText('No organizations yet')).toBeInTheDocument();
  });

  it('shows an error state with a retry when loading fails', async () => {
    vi.mocked(organizationService.list).mockRejectedValueOnce(new Error("Can't reach the server."));
    renderAs(<OrganizationsPage />, [PERMISSIONS.Organization.View]);
    expect(await screen.findByRole('alert')).toHaveTextContent("Can't reach the server.");
    vi.mocked(organizationService.list).mockResolvedValue([org(1, 'NNX')]);
    screen.getByRole('button', { name: 'Try again' }).click();
    await waitFor(() => expect(screen.getByRole('heading', { name: 'NNX' })).toBeInTheDocument());
  });
});
