import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Plus, Building2, Mail, Calendar } from 'lucide-react';
import { useState, useMemo } from 'react';
import { merchantsApi } from '@/api/client';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card';
import { Button } from '@/components/Button';
import { Badge } from '@/components/Badge';
import { LoadingSpinner } from '@/components/Loading';
import { ErrorMessage } from '@/components/ErrorMessage';
import { SearchInput } from '@/components/SearchInput';
import { Pagination } from '@/components/Pagination';
import { usePagination } from '@/hooks/usePagination';
import { useToast } from '@/hooks/useToast';
import { formatDate } from '@/lib/utils';
import type { CreateMerchantRequest } from '@/types';

function CreateMerchantModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const { success, error } = useToast();
  const [formData, setFormData] = useState<CreateMerchantRequest>({
    name: '',
    email: '',
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateMerchantRequest) => merchantsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['merchants'] });
      success('Merchant created successfully!');
      onClose();
      setFormData({ name: '', email: '' });
    },
    onError: () => {
      error('Failed to create merchant. Please try again.');
    },
  });

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
      data-testid="create-merchant-modal"
    >
      <div className="bg-white rounded-lg max-w-md w-full p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Create New Merchant</h2>
        
        <form
          onSubmit={(e) => {
            e.preventDefault();
            createMutation.mutate(formData);
          }}
          className="space-y-4"
          data-testid="create-merchant-form"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Name
            </label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
              placeholder="Acme Corp"
              data-testid="merchant-name-input"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <input
              type="email"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
              placeholder="merchant@example.com"
              data-testid="merchant-email-input"
            />
          </div>

          <div className="flex gap-3 justify-end pt-4">
            <Button
              type="button"
              variant="secondary"
              onClick={onClose}
              disabled={createMutation.isPending}
              data-testid="cancel-button"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={createMutation.isPending}
              data-testid="submit-button"
            >
              {createMutation.isPending ? 'Creating...' : 'Create Merchant'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function Merchants() {
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const { data: merchants, isLoading, error } = useQuery({
    queryKey: ['merchants'],
    queryFn: merchantsApi.getAll,
  });

  // Filter merchants based on search and status
  const filteredMerchants = useMemo(() => {
    if (!merchants) return [];
    
    return merchants.filter((merchant) => {
      const matchesSearch = 
        merchant.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        merchant.email.toLowerCase().includes(searchQuery.toLowerCase());
      
      const matchesStatus = statusFilter === 'all' || merchant.status === statusFilter;
      
      return matchesSearch && matchesStatus;
    });
  }, [merchants, searchQuery, statusFilter]);

  // Pagination
  const {
    currentPage,
    totalPages,
    paginatedItems,
    goToPage,
    totalItems,
    itemsPerPage,
  } = usePagination(filteredMerchants, 12);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <LoadingSpinner />
      </div>
    );
  }

  if (error) {
    return <ErrorMessage message="Failed to load merchants" />;
  }

  return (
    <div className="space-y-6" data-testid="merchants-page">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900" data-testid="page-title">Merchants</h1>
          <p className="mt-1 text-sm text-gray-600">
            Manage your merchant accounts
          </p>
        </div>
        <Button
          onClick={() => setIsCreateModalOpen(true)}
          data-testid="create-merchant-button"
        >
          <Plus className="w-4 h-4 mr-2" />
          New Merchant
        </Button>
      </div>

      {/* Search and Filters */}
      <Card>
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1">
              <SearchInput
                value={searchQuery}
                onChange={setSearchQuery}
                placeholder="Search by name or email..."
              />
            </div>
            <div className="sm:w-48">
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
                data-testid="status-filter"
              >
                <option value="all">All Status</option>
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="Suspended">Suspended</option>
              </select>
            </div>
          </div>
          
          {(searchQuery || statusFilter !== 'all') && (
            <div className="mt-4 text-sm text-gray-600" data-testid="filter-results">
              Found {filteredMerchants.length} merchant{filteredMerchants.length !== 1 ? 's' : ''}
              {searchQuery && ` matching "${searchQuery}"`}
              {statusFilter !== 'all' && ` with status "${statusFilter}"`}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Merchants Grid */}
      {paginatedItems.length === 0 ? (
        <Card>
          <CardContent>
            <div className="text-center py-12" data-testid="empty-state">
              <Building2 className="mx-auto h-12 w-12 text-gray-400" />
              <h3 className="mt-2 text-sm font-medium text-gray-900">
                {searchQuery || statusFilter !== 'all' ? 'No merchants found' : 'No merchants'}
              </h3>
              <p className="mt-1 text-sm text-gray-500">
                {searchQuery || statusFilter !== 'all' 
                  ? 'Try adjusting your search or filters' 
                  : 'Get started by creating a new merchant.'}
              </p>
              {!searchQuery && statusFilter === 'all' && (
                <div className="mt-6">
                  <Button onClick={() => setIsCreateModalOpen(true)}>
                    <Plus className="w-4 h-4 mr-2" />
                    Create Merchant
                  </Button>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      ) : (
        <>
          <div
            className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
            data-testid="merchants-grid"
          >
            {paginatedItems.map((merchant) => (
              <Link
                key={merchant.id}
                to={`/merchants/${merchant.id}`}
                data-testid={`merchant-card-${merchant.id}`}
              >
                <Card className="hover:shadow-lg transition-shadow cursor-pointer h-full">
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-primary-100 flex items-center justify-center">
                          <Building2 className="w-5 h-5 text-primary-600" />
                        </div>
                        <div>
                          <CardTitle className="text-lg" data-testid="merchant-name">
                            {merchant.name}
                          </CardTitle>
                        </div>
                      </div>
                      <Badge status={merchant.status} data-testid="merchant-status">
                        {merchant.status}
                      </Badge>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2 text-sm">
                      <div className="flex items-center text-gray-600">
                        <Mail className="w-4 h-4 mr-2" />
                        <span data-testid="merchant-email">{merchant.email}</span>
                      </div>
                      <div className="flex items-center text-gray-600">
                        <Calendar className="w-4 h-4 mr-2" />
                        <span data-testid="merchant-created">Created {formatDate(merchant.createdAt)}</span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={goToPage}
              totalItems={totalItems}
              itemsPerPage={itemsPerPage}
            />
          )}
        </>
      )}

      {/* Create Modal */}
      <CreateMerchantModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
    </div>
  );
}
