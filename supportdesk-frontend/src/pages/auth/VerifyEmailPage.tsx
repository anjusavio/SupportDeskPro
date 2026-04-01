/**
 * VerifyEmailPage — handles email verification link click.
 *
 * CONCEPTS:
 *
 * 1. useSearchParams — reads ?token=xxx from URL
 *    User clicks link in email → lands here with token in URL 
 *
 * 2. useEffect on mount — auto-calls API immediately
 *    No button needed — verification happens automatically
 *    when page loads 
 *
 * 3. Three states — verifying / success / error
 *    Shows appropriate UI for each state 
 */

import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { CheckCircle2, XCircle, Loader2 } from 'lucide-react';
import axiosClient from '../../api/axiosClient';

type VerifyState = 'verifying' | 'success' | 'error';

const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [state, setState] = useState<VerifyState>('verifying');
  const [message, setMessage] = useState('');

  const token = searchParams.get('token'); // reads ?token=xxx from URL 

  useEffect(() => {
    // Auto-verify on page load — no button needed 
    if (!token) {
      setState('error');
      setMessage('Invalid verification link. Token is missing.');
      return;
    }

    axiosClient
      .post('/auth/verify-email', { token })
      .then(() => {
        setState('success');
        // Auto-redirect to login after 3 seconds
        setTimeout(() => navigate('/login'), 3000);
      })
      .catch((error) => {
        setState('error');
        setMessage(
          error.response?.data?.message ||
          'Verification failed. The link may have expired.'
        );
      });
  }, [token, navigate]); // runs once when token is available 

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-200
                      p-8 w-full max-w-md text-center">

        {/* Verifying state */}
        {state === 'verifying' && (
          <>
            <div className="flex justify-center mb-4">
              <Loader2 size={48} className="text-indigo-600 animate-spin" />
            </div>
            <h1 className="text-xl font-bold text-gray-900 mb-2">
              Verifying your email...
            </h1>
            <p className="text-sm text-gray-500">Please wait a moment.</p>
          </>
        )}

        {/* Success state */}
        {state === 'success' && (
          <>
            <div className="flex justify-center mb-4">
              <CheckCircle2 size={48} className="text-green-500" />
            </div>
            <h1 className="text-xl font-bold text-gray-900 mb-2">
              Email Verified!
            </h1>
            <p className="text-sm text-gray-500 mb-6">
              Your email has been verified successfully.
              Redirecting to login...
            </p>
            <button
              onClick={() => navigate('/login')}
              className="w-full py-2.5 bg-indigo-600 text-white text-sm
                         font-medium rounded-xl hover:bg-indigo-700 transition-colors"
            >
              Go to Login
            </button>
          </>
        )}

        {/* Error state */}
        {state === 'error' && (
          <>
            <div className="flex justify-center mb-4">
              <XCircle size={48} className="text-red-500" />
            </div>
            <h1 className="text-xl font-bold text-gray-900 mb-2">
              Verification Failed
            </h1>
            <p className="text-sm text-gray-500 mb-6">{message}</p>
            <div className="flex flex-col gap-2">
              <button
                onClick={() => navigate('/login')}
                className="w-full py-2.5 bg-indigo-600 text-white text-sm
                           font-medium rounded-xl hover:bg-indigo-700 transition-colors"
              >
                Go to Login
              </button>
              <button
                onClick={() => navigate('/register')}
                className="w-full py-2.5 border border-gray-200 text-gray-600
                           text-sm font-medium rounded-xl hover:bg-gray-50 transition-colors"
              >
                Register Again
              </button>
            </div>
          </>
        )}

      </div>
    </div>
  );
};

export default VerifyEmailPage;